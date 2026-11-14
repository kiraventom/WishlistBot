using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Model;
using WishlistBot.Model.User;

namespace WishlistBot.Profiles;

/// <summary>Итог одного прохода <see cref="BirthdaySyncService"/> — для лога и для тестов.</summary>
public sealed class BirthdaySyncReport
{
    /// <summary>Сколько профилей без даты нашлось — столько и собирались обойти.</summary>
    public int Candidates { get; set; }

    /// <summary>Сколько реально спросили (остаток может не влезть в <see cref="BirthdaySyncService.MaxPerPass"/>).</summary>
    public int Asked { get; set; }

    /// <summary>Сколько дат записали.</summary>
    public int Filled { get; set; }

    /// <summary>В Telegram даты нет (или она там битая, или человек скрыл и день с месяцем).</summary>
    public int WithoutBirthday { get; set; }

    /// <summary>Не записали: дату уже успели указать руками или профиль исчез.</summary>
    public int Skipped { get; set; }

    /// <summary>Профиль не удалось прочитать или записать.</summary>
    public int Failed { get; set; }

    /// <summary>Проход прерван: Telegram подряд отвечал ошибкой — долбить дальше бессмысленно.</summary>
    public bool Aborted { get; set; }

    /// <summary>Проход прерван: выбрали <see cref="BirthdaySyncService.MaxPerPass"/> профилей, остальные — в следующий раз.</summary>
    public bool StoppedByLimit { get; set; }
}

/// <summary>
/// Дата рождения из профиля Telegram. Разово на старте и раз в сутки обходит профили без
/// даты и подставляет её оттуда — если человек сам её в Telegram открыл
/// (<c>getChat</c> → <see cref="ChatFullInfo.Birthdate"/>; год там опционален, и это нормально:
/// «10 марта» тоже дата рождения).
///
/// Главное правило — **не навредить**: лучше оставить профиль без даты, чем уронить проход
/// или записать чушь. Поэтому: одна ошибка на пользователя не рвёт проход; мусорные даты
/// (31 апреля, год из будущего) отбрасываются; уже указанную дату никогда не перезаписываем;
/// подряд идущие ошибки означают «Telegram недоступен» — проход прекращается; за один раз
/// спрашиваем не больше <see cref="MaxPerPass"/> профилей и делаем паузу между запросами.
/// </summary>
public sealed class BirthdaySyncService
{
    /// <summary>Сколько профилей обходим за один проход (остальные — в следующий).</summary>
    public const int MaxPerPass = 300;

    /// <summary>Столько подряд идущих ошибок считаем «Telegram недоступен» и прекращаем проход.</summary>
    public const int MaxConsecutiveFailures = 5;

    /// <summary>
    /// Пауза между запросами. Лимит Telegram — около 30 запросов в секунду на бота, и он
    /// общий с экранами, поэтому берём в разы меньше.
    /// </summary>
    public static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>Во сколько по локальному времени бота делаем суточный проход.</summary>
    public static readonly TimeSpan RunTime = TimeSpan.FromHours(4);

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    private bool _started;
    private ILogger _logger;
    private ITelegramBotClient _client;
    private DateOnly? _lastRunDate;

    public static BirthdaySyncService Instance { get; } = new();

    public void Start(ILogger logger, ITelegramBotClient client)
    {
        if (_started)
            return;

        _started = true;
        _logger = logger;
        _client = client;

        _logger.Information("Birthday sync service started");
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
    /// Один проход по профилям без даты. <paramref name="readBirthday"/> — откуда брать дату
    /// по telegram id (null — в Telegram её нет); исключение означает «спросить не удалось».
    /// Вынесено в делегат, чтобы проход можно было проверить без Telegram.
    /// </summary>
    public async Task<BirthdaySyncReport> RunPassAsync(
        Func<long, Task<Birthday?>> readBirthday,
        CancellationToken cancellationToken = default)
    {
        var report = new BirthdaySyncReport();
        var candidates = LoadCandidates();
        report.Candidates = candidates.Count;

        var consecutiveFailures = 0;

        foreach (var candidate in candidates)
        {
            if (report.Asked >= MaxPerPass)
            {
                report.StoppedByLimit = true;
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            ++report.Asked;

            Birthday? birthday;

            try
            {
                birthday = await readBirthday(candidate.TelegramId);
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
                    "Couldn't read Telegram profile of user {userId}: {error}", candidate.UserId, e.Message);

                // Подряд идущие ошибки — это уже не отдельный заблокировавший бота человек,
                // а недоступный Telegram: останавливаемся, иначе завалим лог и лимиты
                if (consecutiveFailures >= MaxConsecutiveFailures)
                {
                    report.Aborted = true;
                    break;
                }

                continue;
            }

            // Мусор отбрасываем здесь, а не только в адаптере Telegram: записать дату
            // вроде «31 апреля» в профиль хуже, чем оставить его пустым
            if (birthday is { } raw)
                birthday = BirthdayUtils.ToBirthday(raw.Day, raw.Month, raw.Year);

            if (birthday is not { } value)
            {
                ++report.WithoutBirthday;
            }
            else
            {
                switch (TrySave(candidate.UserId, value))
                {
                    case SaveOutcome.Saved:
                        ++report.Filled;
                        break;
                    case SaveOutcome.Failed:
                        ++report.Failed;
                        break;
                    default:
                        // Профиля уже нет или дату успели указать руками — чужое не перезаписываем
                        ++report.Skipped;
                        break;
                }
            }

            if (RequestDelay > TimeSpan.Zero)
                await Task.Delay(RequestDelay, cancellationToken);
        }

        return report;
    }

    /// <summary>Профили без даты рождения: день и месяц либо оба есть, либо оба пусты.</summary>
    private static List<(int UserId, long TelegramId)> LoadCandidates()
    {
        using var userContext = UserContext.Create();

        return userContext.Users
            .AsNoTracking()
            .Where(user => user.Profile == null || user.Profile.BirthdayDay == null || user.Profile.BirthdayMonth == null)
            .OrderBy(user => user.UserId)
            .Select(user => new { user.UserId, user.TelegramId })
            .ToList()
            .Select(user => (user.UserId, user.TelegramId))
            .ToList();
    }

    private enum SaveOutcome
    {
        Saved,
        AlreadySet,
        Failed
    }

    /// <summary>
    /// Записывает дату в профиль, если он всё ещё без неё. Свой контекст на пользователя:
    /// падение на одном профиле не тянет за собой остальные.
    /// </summary>
    private SaveOutcome TrySave(int userId, Birthday birthday)
    {
        try
        {
            using var userContext = UserContext.Create();

            var profile = userContext.Users
                .Include(user => user.Profile)
                .FirstOrDefault(user => user.UserId == userId)
                ?.Profile;

            if (profile is null || profile.Birthday is not null)
                return SaveOutcome.AlreadySet;

            profile.Birthday = birthday;
            userContext.SaveChanges();

            _logger?.Debug("Birthday of user {userId} is taken from Telegram: {birthday}", userId, birthday);
            return SaveOutcome.Saved;
        }
        catch (Exception e)
        {
            _logger?.Warning("Couldn't save birthday of user {userId}: {error}", userId, e.Message);
            return SaveOutcome.Failed;
        }
    }

    /// <summary>Дата рождения из профиля Telegram; null — человек её не показывал.</summary>
    private async Task<Birthday?> ReadFromTelegram(long telegramId, CancellationToken cancellationToken)
    {
        var chat = await _client.GetChat(new ChatId(telegramId), cancellationToken);
        var birthdate = chat.Birthdate;

        if (birthdate is null)
            return null;

        return BirthdayUtils.ToBirthday(birthdate.Day, birthdate.Month, birthdate.Year);
    }

    private async Task RunLoop()
    {
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
            var report = await RunPassAsync(telegramId => ReadFromTelegram(telegramId, CancellationToken.None));

            _logger?.Information(
                "Birthday sync: asked {asked} of {candidates} profile(s), filled {filled}, " +
                "no birthday in Telegram {without}, already set {skipped}, failed {failed}{aborted}{limit}",
                report.Asked,
                report.Candidates,
                report.Filled,
                report.WithoutBirthday,
                report.Skipped,
                report.Failed,
                report.Aborted ? ", aborted (Telegram is unavailable)" : string.Empty,
                report.StoppedByLimit ? $", limit {MaxPerPass} reached" : string.Empty);
        }
        catch (Exception e)
        {
            _logger?.Error("Birthday sync failed: {error}", e.ToString());
        }
    }
}
