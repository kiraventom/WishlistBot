using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Model.User;

namespace WishlistBot.Profiles;

/// <summary>Что бот знает о человеке из профиля Telegram: имя и тэг (<c>@username</c>).</summary>
public sealed record TelegramProfile(string FirstName, string Tag);

/// <summary>Итог одного прохода <see cref="ProfileSyncService"/> — для лога и для проверок.</summary>
public sealed class ProfileSyncReport
{
    /// <summary>Сколько человек спросили у Telegram.</summary>
    public int Asked { get; set; }

    /// <summary>Сколько раз поменялось имя.</summary>
    public int Renamed { get; set; }

    /// <summary>Сколько раз поменялся тэг (в том числе «тэг убрали»).</summary>
    public int Retagged { get; set; }

    /// <summary>Профиль не удалось прочитать или записать.</summary>
    public int Failed { get; set; }

    /// <summary>Проход прерван: Telegram подряд отвечал ошибкой — долбить дальше бессмысленно.</summary>
    public bool Aborted { get; set; }
}

/// <summary>
/// Имя и тэг из профиля Telegram. Бот узнаёт их только из апдейта, а человек меняет имя
/// и <c>@username</c> когда захочет — боту об этом никто не сообщает. Поэтому раз в сутки
/// (и на старте) идём <c>getChat</c> по каждому пользователю и сверяем
/// <see cref="UserModel.FirstName"/> и <see cref="UserModel.Tag"/>.
///
/// Механика та же, что у аватарок (<see cref="WishlistBot.Media.AvatarPackService"/>): свой
/// проход по <see cref="UserModel.ProfileCheckedAt"/>, порядок «кого дольше не проверяли»,
/// отметка ставится даже при ошибке. Только отдельная от аватарок: те требуют Premium
/// у владельца бота и живут в стикерах, а имя с тэгом нужны всегда.
///
/// Главное правило — не навредить: пустое имя от Telegram не затирает сохранённое, одна
/// ошибка на человека не рвёт проход, а несколько подряд идущих ошибок означают
/// «Telegram недоступен» — проход прекращается.
/// </summary>
public class ProfileSyncService
{
    /// <summary>Сколько пользователей спрашиваем за один запрос (один <c>getChat</c> на человека).</summary>
    public const int BatchSize = 50;

    /// <summary>Предохранитель от бесконечного цикла, если Telegram начнёт отказывать на всём.</summary>
    public const int MaxBatches = 200;

    /// <summary>Столько подряд идущих ошибок считаем «Telegram недоступен» и прекращаем проход.</summary>
    public const int MaxConsecutiveFailures = 5;

    /// <summary>Как часто переспрашиваем Telegram про имя и тэг.</summary>
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// Пауза между запросами. Лимит Telegram — около 30 запросов в секунду на бота, и он
    /// общий с экранами, поэтому берём в разы меньше.
    /// </summary>
    public static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>Пауза между пачками внутри прохода.</summary>
    public static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(1);

    /// <summary>Во сколько по локальному времени бота делаем суточный проход.</summary>
    public static readonly TimeSpan RunTime = TimeSpan.FromHours(3);

    /// <summary>
    /// Задержка первого прохода после старта: одновременно с ним стартуют дата рождения
    /// (тоже <c>getChat</c> по всем профилям) и догрузка миниатюр — незачем им мешать.
    /// </summary>
    public static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    private bool _started;
    private ILogger _logger;
    private ITelegramBotClient _client;
    private DateOnly? _lastRunDate;

    public static ProfileSyncService Instance { get; } = new();

    public void Start(ILogger logger, ITelegramBotClient client)
    {
        if (_started)
            return;

        _started = true;
        _logger = logger;
        _client = client;

        _logger.Information("Profile sync service started");
        _ = Task.Run(RunLoop);
    }

    /// <summary>
    /// Пора ли запускать суточный проход: раз в день и не раньше <see cref="RunTime"/>.
    /// Разовый проход на старте считается отдельно — он уже прошёл.
    /// </summary>
    public bool IsRunDue(DateTime now) =>
        now.TimeOfDay >= RunTime && _lastRunDate != DateOnly.FromDateTime(now);

    /// <summary>Отметить суточный проход сделанным (иначе он повторится на следующем тике).</summary>
    public void MarkRun(DateTime now) => _lastRunDate = DateOnly.FromDateTime(now);

    /// <summary>
    /// Один проход: пачками берёт тех, кого давно не проверяли, и сверяет имя с тэгом.
    /// <paramref name="readProfile"/> — откуда брать имя и тэг по telegram id; исключение
    /// означает «спросить не удалось». Вынесено в делегат, чтобы проход можно было
    /// проверить без Telegram.
    /// </summary>
    public async Task<ProfileSyncReport> RunPassAsync(
        Func<long, Task<TelegramProfile>> readProfile,
        CancellationToken cancellationToken = default)
    {
        var report = new ProfileSyncReport();
        var consecutiveFailures = 0;

        for (var batch = 0; batch < MaxBatches; ++batch)
        {
            var users = LoadStale(BatchSize);

            // Проход заканчивается, когда устаревших не осталось: все, кого спросили,
            // получили свежий ProfileCheckedAt и ушли в конец очереди
            if (users.Count == 0)
                break;

            foreach (var (userId, telegramId) in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ++report.Asked;

                TelegramProfile profile;

                try
                {
                    profile = await readProfile(telegramId);
                    consecutiveFailures = 0;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ++report.Failed;
                    ++consecutiveFailures;

                    _logger?.Warning(
                        "Couldn't read Telegram profile of user {userId}: {error}", userId, e.Message);

                    // Отметку ставим и на ошибке: иначе один недоступный профиль
                    // (удалил аккаунт, заблокировал бота) будет возвращаться каждый проход
                    TouchCheckedAt(userId);

                    if (consecutiveFailures >= MaxConsecutiveFailures)
                    {
                        report.Aborted = true;
                        break;
                    }

                    if (RequestDelay > TimeSpan.Zero)
                        await Task.Delay(RequestDelay, cancellationToken);

                    continue;
                }

                var saved = TrySave(userId, profile);

                if (saved.Renamed)
                    ++report.Renamed;

                if (saved.Retagged)
                    ++report.Retagged;

                if (RequestDelay > TimeSpan.Zero)
                    await Task.Delay(RequestDelay, cancellationToken);
            }

            if (report.Aborted)
                break;

            if (BatchDelay > TimeSpan.Zero)
                await Task.Delay(BatchDelay, cancellationToken);
        }

        return report;
    }

    /// <summary>Что именно поменялось при сверке одного профиля.</summary>
    private readonly record struct SaveResult(bool Renamed, bool Retagged)
    {
        public static SaveResult Nothing => new(false, false);
    }

    /// <summary>Пользователи, которых давно не сверяли: кого дольше всех — первыми.</summary>
    private static List<(int UserId, long TelegramId)> LoadStale(int batchSize)
    {
        var cutoff = DateTime.UtcNow - RefreshInterval;

        using var userContext = UserContext.Create();

        return userContext.Users
            .AsNoTracking()
            .Where(user => user.ProfileCheckedAt == null || user.ProfileCheckedAt < cutoff)
            .OrderBy(user => user.ProfileCheckedAt ?? DateTime.MinValue)
            .Take(batchSize)
            .Select(user => new { user.UserId, user.TelegramId })
            .ToList()
            .Select(user => (user.UserId, user.TelegramId))
            .ToList();
    }

    /// <summary>
    /// Записывает имя и тэг, если они изменились, и ставит отметку сверки. Свой контекст
    /// на пользователя: падение на одном профиле не тянет за собой остальные.
    /// </summary>
    private SaveResult TrySave(int userId, TelegramProfile profile)
    {
        try
        {
            using var userContext = UserContext.Create();

            var user = userContext.Users.FirstOrDefault(row => row.UserId == userId);

            if (user is null)
                return SaveResult.Nothing;

            var renamed = false;

            // Пустое имя от Telegram не затирает сохранённое: имя в базе обязательно,
            // а «без имени» человек выглядел бы в списках хуже, чем со старым
            if (!string.IsNullOrWhiteSpace(profile.FirstName) && user.FirstName != profile.FirstName)
            {
                _logger?.Information("Name of [{userId}] is updated: '{old}' → '{new}'",
                    userId, user.FirstName, profile.FirstName);
                user.FirstName = profile.FirstName;
                renamed = true;
            }

            var retagged = false;

            // Тэг меняется и на «убран в Telegram»: тогда упоминание падает на tg://user?id=
            if (user.Tag != profile.Tag)
            {
                _logger?.Information("Tag of [{userId}] is updated: '{old}' → '{new}'",
                    userId, user.Tag, profile.Tag);
                user.Tag = profile.Tag;
                retagged = true;
            }

            user.ProfileCheckedAt = DateTime.UtcNow;
            userContext.SaveChanges();

            return new SaveResult(renamed, retagged);
        }
        catch (Exception e)
        {
            _logger?.Warning("Couldn't save profile of user {userId}: {error}", userId, e.Message);
            return SaveResult.Nothing;
        }
    }

    /// <summary>Отметка сверки без изменений: строка должна уйти в конец очереди в любом случае.</summary>
    private static void TouchCheckedAt(int userId)
    {
        try
        {
            using var userContext = UserContext.Create();

            var user = userContext.Users.FirstOrDefault(row => row.UserId == userId);

            if (user is null)
                return;

            user.ProfileCheckedAt = DateTime.UtcNow;
            userContext.SaveChanges();
        }
        catch
        {
            // Отметить не удалось — не беда: пользователь просто вернётся в следующем проходе
        }
    }

    /// <summary>Имя и тэг из профиля Telegram.</summary>
    private async Task<TelegramProfile> ReadFromTelegram(long telegramId, CancellationToken cancellationToken)
    {
        var chat = await _client.GetChat(new ChatId(telegramId), cancellationToken);

        return new TelegramProfile(chat.FirstName, chat.Username);
    }

    private async Task RunLoop()
    {
        await Task.Delay(StartupDelay);

        await RunPassLoggedAsync();

        while (true)
        {
            await Task.Delay(CheckInterval);

            if (!IsRunDue(DateTime.Now))
                continue;

            MarkRun(DateTime.Now);
            await RunPassLoggedAsync();
        }
    }

    private async Task RunPassLoggedAsync()
    {
        try
        {
            var report = await RunPassAsync(
                telegramId => ReadFromTelegram(telegramId, CancellationToken.None));

            _logger?.Information(
                "Profile sync: asked {asked}, renamed {renamed}, retagged {retagged}, failed {failed}{aborted}",
                report.Asked,
                report.Renamed,
                report.Retagged,
                report.Failed,
                report.Aborted ? ", aborted (Telegram is unavailable)" : string.Empty);
        }
        catch (Exception e)
        {
            _logger?.Error("Profile sync failed: {error}", e.ToString());
        }
    }
}
