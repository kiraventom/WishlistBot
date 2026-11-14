using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WishlistBot.Model;
using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.Media;

/// <summary>
/// Миниатюры вишей как кастомные эмодзи: картинка виша превращается в стикер 100×100,
/// а в ячейке таблицы вместо маркера 🖼️ рисуется <c>![](tg://emoji?id=…)</c>.
///
/// Почему именно так: картинку в ячейку таблицы Telegram не пускает вообще — ни
/// <c>tg://photo</c>, ни <c>&lt;img&gt;</c>, он молча её выбрасывает (проверено:
/// <c>rich-probe/table_image_probe.py</c>). Внутри ячейки живёт только инлайн-разметка,
/// а единственный рисованный инлайн — кастомный эмодзи.
///
/// Набор на пользователя: бот создаёт его сам (<c>createNewStickerSet</c> со
/// <c>sticker_type=custom_emoji</c>), владелец — пользователь, поэтому набор виден
/// у него в клиенте. Эмодзи стикера — 🖼️: Telegram берёт его как <c>alternative_text</c>
/// и показывает вместо картинки там, где кастомные эмодзи не рисуются.
///
/// Сеть трогаем только после коммита апдейта (<see cref="FlushAsync"/>), как и импорт
/// блобов: внутри транзакции апдейта сетевых вызовов быть не должно.
/// </summary>
public class EmojiPackService
{
    /// <summary>Лимит Telegram на emoji-набор. Держим в голове: вишей у пользователя ≤ 100.</summary>
    public const int MaxStickersPerPack = 200;

    /// <summary>Сколько раз пробуем сделать миниатюру, прежде чем оставить виш с маркером 🖼️.</summary>
    private const int MaxThumbAttempts = 3;

    /// <summary>Телеграм отверг эмодзи — на столько выключаем миниатюры, потом пробуем снова.</summary>
    public static readonly TimeSpan DisableOnFailure = TimeSpan.FromHours(24);

    private readonly object _queueLock = new();
    private readonly List<string> _queuedKeys = [];

    private bool _inited;
    private ILogger _logger;
    private ITelegramBotClient _client;

    /// <summary>
    /// Выключатель миниатюр. Гасится, когда Telegram отверг эмодзи: он валит всё сообщение
    /// (<c>RICH_MESSAGE_EMOJI_DOC_INVALID</c>), поэтому лучше показать маркеры, чем не
    /// показать экран вообще. Через сутки включается сам — это и есть проверка «а вдруг
    /// Premium вернулся»: набор при этом цел, ломается именно отправка.
    /// </summary>
    private volatile bool _enabled = true;
    private DateTime _retryAt = DateTime.MinValue;

    public static EmojiPackService Instance { get; } = new();

    public bool IsInited => _inited;

    public bool Enabled
    {
        get
        {
            if (_enabled)
                return true;

            if (DateTime.UtcNow < _retryAt)
                return false;

            _enabled = true;
            return true;
        }
    }

    public void Init(ILogger logger, ITelegramBotClient client)
    {
        if (_inited)
            return;

        _logger = logger;
        _client = client;
        _inited = true;
    }

    /// <summary>Выключает миниатюры: следующий экран придёт с маркерами 🖼️.</summary>
    public void Disable()
    {
        _enabled = false;
        _retryAt = DateTime.UtcNow + DisableOnFailure;
        _logger?.Warning("Thumbnails are disabled until {retryAt} (UTC): Telegram rejected an emoji id", _retryAt.ToString("u"));
    }

    /// <summary>Включает миниатюры обратно (после того как Telegram перестал ругаться).</summary>
    public void Enable()
    {
        _enabled = true;
        _retryAt = DateTime.MinValue;
    }

    /// <summary>Просит собрать миниатюру для ключа сразу после коммита апдейта.</summary>
    public void QueueThumbnail(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        lock (_queueLock)
        {
            if (!_queuedKeys.Contains(key))
                _queuedKeys.Add(key);
        }
    }

    /// <summary>Собирает всё, что накопилось в очереди. Вызывается после коммита.</summary>
    public async Task FlushAsync()
    {
        List<string> keys;

        lock (_queueLock)
        {
            if (_queuedKeys.Count == 0)
                return;

            keys = [.. _queuedKeys];
            _queuedKeys.Clear();
        }

        foreach (var key in keys)
        {
            // Одна картинка не должна ронять весь апдейт: ошибки внутри уже пойманы,
            // но база тоже может отказать
            try
            {
                await BuildThumbnailAsync(key);
            }
            catch (Exception e)
            {
                _logger?.Warning("Failed to build a thumbnail for media '{key}': {error}", key, e.Message);
            }
        }
    }

    /// <summary>
    /// Догружает миниатюры для картинок, у которых их ещё нет: старые виши, картинки,
    /// импортированные фоном, и всё, что не успело собраться.
    /// </summary>
    /// <returns>
    /// Сколько картинок **попробовали** (а не сколько собралось). Иначе догрузка
    /// застревает: первые в очереди картинки могут быть безхозными, и на «собралось = 0»
    /// цикл решал бы, что делать больше нечего. Каждая попытка увеличивает счётчик,
    /// поэтому неудачные строки сами выпадают из очереди через три захода.
    /// </returns>
    public async Task<int> BackfillAsync(int batchSize)
    {
        if (!_inited || !Enabled)
            return 0;

        List<string> keys;

        using (var storageContext = MediaStorageContext.Create())
        {
            keys = storageContext.StoredMedia
                .Where(m => m.CustomEmojiId == null
                    && m.Data != null
                    && m.Width >= ImageCompressor.EmojiSize
                    && m.Height >= ImageCompressor.EmojiSize
                    && m.ThumbAttempts < MaxThumbAttempts)
                .OrderBy(m => m.MediaItemId)
                .Take(batchSize)
                .Select(m => m.Key)
                .ToList();
        }

        foreach (var key in keys)
        {
            if (!Enabled)
                break;

            try
            {
                await BuildThumbnailAsync(key);
            }
            catch (Exception e)
            {
                _logger?.Warning("Failed to build a thumbnail for media '{key}': {error}", key, e.Message);
            }
        }

        return keys.Count;
    }

    /// <summary>
    /// Помечает картинки, которые не принадлежат ни вишу, ни черновику (рассылки, хвосты
    /// удалённых вишей, старый импорт), как «миниатюра не нужна».
    ///
    /// Без этого догрузка стоит на месте: в очереди они идут первыми по id, собрать из них
    /// миниатюру невозможно, и каждый проход упирается в те же строки — именно так на старте
    /// было «Failed to make a thumbnail … no wish owns this picture» вместо настоящих вишей.
    /// </summary>
    /// <returns>Сколько картинок помечено.</returns>
    public async Task<int> MarkOwnerlessMediaAsync()
    {
        HashSet<string> owned;

        using (var userContext = UserContext.Create())
        {
            owned = userContext.Wishes
                .Where(wish => wish.MediaKey != null)
                .Select(wish => wish.MediaKey)
                .Concat(userContext.WishDrafts
                    .Where(draft => draft.MediaKey != null)
                    .Select(draft => draft.MediaKey))
                .ToHashSet();
        }

        List<int> orphanIds;
        int pending, withoutBlob, tooSmall;

        using (var storageContext = MediaStorageContext.Create())
        {
            var candidates = storageContext.StoredMedia
                .Where(m => m.CustomEmojiId == null && m.ThumbAttempts < MaxThumbAttempts)
                .Select(m => new { m.MediaItemId, m.Key, m.Width, m.Height, HasBlob = m.Data != null })
                .ToList();

            pending = candidates.Count;
            withoutBlob = candidates.Count(c => !c.HasBlob);
            tooSmall = candidates.Count(c => c.HasBlob
                && (c.Width < ImageCompressor.EmojiSize || c.Height < ImageCompressor.EmojiSize));
            orphanIds = candidates
                .Where(candidate => !owned.Contains(candidate.Key))
                .Select(candidate => candidate.MediaItemId)
                .ToList();
        }

        if (orphanIds.Count > 0)
        {
            using var storageContext = MediaStorageContext.Create();

            // Пачками: список параметров в SQLITE ограничен
            foreach (var chunk in orphanIds.Chunk(200))
            {
                var ids = chunk.ToList();
                await storageContext.StoredMedia
                    .Where(m => ids.Contains(m.MediaItemId))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.ThumbAttempts, MaxThumbAttempts));
            }
        }

        _logger?.Information(
            "Thumbnails: {pending} picture(s) in queue, {orphans} belong to no wish, {withoutBlob} have no blob yet, {tooSmall} have no size or are smaller than {size}px",
            pending, orphanIds.Count, withoutBlob, tooSmall, ImageCompressor.EmojiSize);

        return orphanIds.Count;
    }

    /// <summary>
    /// Суточная проверка наборов: жив ли набор в Telegram. Пропавший набор обнуляем —
    /// миниатюры соберутся заново в новый набор (старые emoji id к этому моменту уже
    /// мертвы, и экраны с ними не уходят).
    /// </summary>
    /// <param name="retryFailed">
    /// Дать второй шанс миниатюрам, которые не собрались раньше. Делаем только в суточном
    /// проходе: быстрые проходы после старта не должны бесконечно повторять одно и то же.
    /// </param>
    public async Task CheckPacksAsync(bool retryFailed = true)
    {
        if (!_inited)
            return;

        using var userContext = UserContext.Create();

        var packs = userContext.EmojiPacks
            .Where(p => p.SetName != null)
            .ToList();

        foreach (var pack in packs)
        {
            try
            {
                var set = await _client.GetStickerSet(pack.SetName);

                pack.IsBroken = false;
                pack.StickerCount = set.Stickers.Count();
                pack.LastCheckedAt = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                _logger?.Warning("Emoji pack '{setName}' of user [{userId}] is gone ({error}), it will be rebuilt",
                    pack.SetName, pack.UserId, e.Message);

                pack.IsBroken = true;
                pack.LastCheckedAt = DateTime.UtcNow;
                pack.SetName = null;
                pack.StickerCount = 0;

                ResetThumbnails(pack.UserId);
            }
        }

        userContext.SaveChanges();

        if (retryFailed)
            RetryFailedThumbnails();
    }

    /// <summary>
    /// Даёт второй шанс миниатюрам, которые не собрались в прошлые сутки: если Telegram
    /// отказывал по своим причинам (не было Premium, лимит стикеров, сбой), ждать вечно
    /// нельзя, а долбить на каждом апдейте — тем более. Раз в сутки, три попытки за сутки.
    /// </summary>
    private void RetryFailedThumbnails()
    {
        using var storageContext = MediaStorageContext.Create();

        var failed = storageContext.StoredMedia
            .Where(m => m.CustomEmojiId == null && m.ThumbAttempts >= MaxThumbAttempts)
            .ToList();

        if (failed.Count == 0)
            return;

        foreach (var item in failed)
            item.ThumbAttempts = 0;

        storageContext.SaveChanges();

        _logger?.Information("Giving another chance to {count} thumbnail(s) that failed before", failed.Count);
    }

    /// <summary>Убирает стикер из набора: виш удалили или заменили ему фото.</summary>
    public async Task DeleteStickerAsync(MediaItemModel mediaItem)
    {
        if (mediaItem is null || string.IsNullOrEmpty(mediaItem.CustomEmojiFileId))
            return;

        if (!_inited || _client is null)
            return;

        try
        {
            await _client.DeleteStickerFromSet(InputFile.FromFileId(mediaItem.CustomEmojiFileId));
            _logger?.Information("Removed thumbnail sticker of media '{key}' from the pack of user [{userId}]",
                mediaItem.Key, mediaItem.EmojiOwnerUserId);
        }
        catch (Exception e)
        {
            // Стикер останется мусором в наборе до суточной сверки — она пересчитает счётчик
            _logger?.Warning("Failed to remove thumbnail sticker of media '{key}': {error}", mediaItem.Key, e.Message);
        }
    }

    /// <summary>Собирает одну миниатюру. true — стикер создан и id записан в медиа-базу.</summary>
    private async Task<bool> BuildThumbnailAsync(string key)
    {
        if (!_inited || _client is null || !Enabled)
            return false;

        MediaItemModel mediaItem;

        using (var storageContext = MediaStorageContext.Create())
            mediaItem = storageContext.StoredMedia.FirstOrDefault(m => m.Key == key);

        if (mediaItem is null || mediaItem.Data is null || mediaItem.CustomEmojiId is not null)
            return false;

        if (mediaItem.ThumbAttempts >= MaxThumbAttempts)
            return false;

        // Блоб-превью (легаси): квадрат 100×100 из него не вырезать. Это не неудача —
        // после апгрейда блоба ключ снова попадёт в очередь и миниатюра соберётся.
        if (Math.Min(mediaItem.Width, mediaItem.Height) < ImageCompressor.EmojiSize)
            return false;

        var owner = FindOwner(key);

        // Ключ не у виша и не у черновика (например, картинка рассылки) — миниатюра
        // не нужна. Считаем попытку, иначе фоновый догрузчик выбирал бы этот ключ
        // в каждой пачке и крутился бы вечно.
        if (owner is null)
        {
            CountAttempt(mediaItem.MediaItemId, key, "no wish owns this picture");
            return false;
        }

        var thumbnail = ImageCompressor.CompressEmoji(mediaItem.Data);

        if (thumbnail is null)
        {
            CountAttempt(mediaItem.MediaItemId, key, "the blob is not a picture");
            return false;
        }

        var setName = EnsureSetName(owner);

        if (string.IsNullOrEmpty(setName))
        {
            // Пользователь пропал из базы — иначе догрузка крутилась бы на этой строке вечно
            CountAttempt(mediaItem.MediaItemId, key, "the wish owner is not in the database");
            return false;
        }

        try
        {
            using var stream = new MemoryStream(thumbnail);

            var uploaded = await _client.UploadStickerFile(
                userId: owner.TelegramId,
                sticker: new InputFileStream(stream, "thumb.webp"),
                stickerFormat: StickerFormat.Static);

            var sticker = await AddStickerAsync(owner, setName, uploaded.FileId);

            if (sticker is null)
            {
                CountAttempt(mediaItem.MediaItemId, key, "Telegram did not return the added sticker");
                return false;
            }

            using (var storageContext = MediaStorageContext.Create())
            {
                var stored = storageContext.StoredMedia.First(m => m.MediaItemId == mediaItem.MediaItemId);

                stored.CustomEmojiId = sticker.CustomEmojiId;
                stored.CustomEmojiFileId = sticker.FileId;
                stored.EmojiOwnerUserId = owner.UserId;
                stored.ThumbAttempts = 0;

                storageContext.SaveChanges();
            }

            _logger?.Information("Thumbnail for media '{key}' is made: emoji [{emojiId}] in '{setName}'",
                key, sticker.CustomEmojiId, setName);

            return true;
        }
        catch (Exception e)
        {
            CountAttempt(mediaItem.MediaItemId, key, e.Message);
            return false;
        }
    }

    /// <summary>
    /// Добавляет стикер в набор (или создаёт набор, если его ещё нет) и возвращает стикер
    /// с его emoji id.
    ///
    /// Искать добавленный стикер по file_id, который вернула загрузка, нельзя: Telegram
    /// кладёт в набор файл с **другим** file_id (проверено живой отправкой:
    /// <c>rich-probe/sticker_file_id_probe.py</c>). Поэтому сравниваем набор до и после
    /// и берём стикер, которого раньше не было — по file_unique_id.
    /// </summary>
    private async Task<Sticker> AddStickerAsync(WishOwner owner, string setName, string uploadedFileId)
    {
        var sticker = new InputSticker(
            InputFile.FromFileId(uploadedFileId),
            StickerFormat.Static,
            [MessageTextUtils.WishThumbnailEmoji]);

        var before = await TryGetSetAsync(setName);
        var known = before is null
            ? []
            : before.Stickers.Select(s => s.FileUniqueId).ToHashSet();

        if (before is null)
            await _client.CreateNewStickerSet(
                userId: owner.TelegramId,
                name: setName,
                title: BuildSetTitle(owner.FirstName),
                stickers: [sticker],
                stickerType: StickerType.CustomEmoji);
        else
            await _client.AddStickerToSet(owner.TelegramId, setName, sticker);

        var after = await TryGetSetAsync(setName);

        return after?.Stickers.FirstOrDefault(s => s.CustomEmojiId is not null && !known.Contains(s.FileUniqueId));
    }

    private async Task<StickerSet> TryGetSetAsync(string setName)
    {
        try
        {
            return await _client.GetStickerSet(setName);
        }
        catch (ApiRequestException e) when (e.ErrorCode == 400)
        {
            return null;
        }
    }

    /// <summary>
    /// Имя набора: <c>w&lt;telegram_id в hex&gt;_by_&lt;bot_username&gt;</c>. Telegram требует
    /// латиницу, цифры и подчёркивания, обязательный суффикс с именем бота и не больше
    /// 64 символов.
    /// </summary>
    public static string BuildSetName(long telegramId) =>
        $"w{telegramId:x}_by_{Config.Instance.Username}";

    private static string BuildSetTitle(string firstName) =>
        string.IsNullOrWhiteSpace(firstName) ? "Вишлист: миниатюры" : $"Вишлист: {firstName}";

    /// <summary>
    /// Находит владельца виша (или черновика) , к которому привязан ключ картинки.
    /// Черновик тоже считаем: фото сначала живёт в нём, и миниатюра успевает собраться
    /// до сохранения виша. Если черновик выбросят — стикер уйдёт вместе с блобом.
    /// </summary>
    private static WishOwner FindOwner(string key)
    {
        using var userContext = UserContext.Create();

        var owner = userContext.Wishes
            .Where(w => w.MediaKey == key)
            .Select(w => new { w.OwnerId, w.Owner.TelegramId, w.Owner.FirstName })
            .FirstOrDefault();

        if (owner is null)
        {
            owner = userContext.WishDrafts
                .Where(d => d.MediaKey == key)
                .Select(d => new { d.OwnerId, d.Owner.TelegramId, d.Owner.FirstName })
                .FirstOrDefault();
        }

        return owner is null ? null : new WishOwner(owner.OwnerId, owner.TelegramId, owner.FirstName);
    }

    /// <summary>Имя набора пользователя; создаёт строку, если её ещё нет.</summary>
    private static string EnsureSetName(WishOwner owner)
    {
        using var userContext = UserContext.Create();

        var user = userContext.Users
            .Include(u => u.EmojiPack)
            .FirstOrDefault(u => u.UserId == owner.UserId);

        if (user is null)
            return null;

        var pack = user.EmojiPack;

        if (pack is null)
        {
            pack = new WishEmojiPackModel() { CreatedAt = DateTime.UtcNow };
            user.EmojiPack = pack;
        }

        if (string.IsNullOrEmpty(pack.SetName))
        {
            pack.SetName = BuildSetName(user.TelegramId);
            pack.IsBroken = false;
            userContext.SaveChanges();
        }

        return pack.SetName;
    }

    /// <summary>Обнуляет миниатюры пользователя: набор пропал, стикеров больше нет.</summary>
    private void ResetThumbnails(int userId)
    {
        using var storageContext = MediaStorageContext.Create();

        var items = storageContext.StoredMedia
            .Where(m => m.EmojiOwnerUserId == userId)
            .ToList();

        foreach (var item in items)
        {
            item.CustomEmojiId = null;
            item.CustomEmojiFileId = null;
            item.EmojiOwnerUserId = null;
            item.ThumbAttempts = 0;
        }

        storageContext.SaveChanges();
    }

    private void CountAttempt(int mediaItemId, string key, string reason)
    {
        using var storageContext = MediaStorageContext.Create();

        var stored = storageContext.StoredMedia.First(m => m.MediaItemId == mediaItemId);
        ++stored.ThumbAttempts;
        storageContext.SaveChanges();

        _logger?.Warning("Failed to make a thumbnail for media '{key}' ({reason}), attempt {attempt}",
            key, reason, stored.ThumbAttempts);
    }

    private sealed record WishOwner(int UserId, long TelegramId, string FirstName);
}
