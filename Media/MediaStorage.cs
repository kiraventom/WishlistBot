using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Model;
using WishlistBot.Model.User;

namespace WishlistBot.Media;

/// <summary>
/// Хранилище картинок вишей и рассылок.
///
/// Файл лежит блобом в медиа-базе (сжатый JPEG), а telegram file_id — только кэш:
/// пока Telegram его принимает, картинку не перезаливаем; если перестал (файл удалён,
/// сменился токен бота) — заливаем блоб заново и обновляем кэш. Канал-хранилище больше
/// не используется: он нужен был только чтобы file_id не умирал вместе с сообщением
/// пользователя, а теперь у нас есть своя копия файла.
/// </summary>
public class MediaStorage
{
    private bool _inited;

    private ILogger _logger;
    private ITelegramBotClient _client;
    private long _storageChannelId;

    /// <summary>
    /// Ключи, которые апдейт освободил (удалили виш или выбросили черновик). Проверяются
    /// после коммита: до него удалённые строки ещё видны в базе, и проверка «никто больше
    /// не ссылается» соврала бы.
    /// </summary>
    private static readonly List<string> _releasedKeys = [];

    /// <summary>Только что загруженные картинки: их надо скачать и сжать сразу после коммита.</summary>
    private static readonly List<string> _uploadedKeys = [];

    /// <summary>
    /// Ключи, у которых блоб — превью, а Telegram показал размер покрупнее: перекачать надо
    /// после коммита (ключ → file_id крупного размера и его размеры).
    /// </summary>
    private static readonly Dictionary<string, (string FileId, int Width, int Height)> _upgradeQueue = [];

    public static MediaStorage Instance { get; } = new();

    public void Init(ILogger logger, ITelegramBotClient client, long storageChannelId)
    {
        if (_inited)
            return;

        _logger = logger;
        _client = client;
        _storageChannelId = storageChannelId;
        _inited = true;
    }

    /// <summary>
    /// Регистрирует присланное пользователем фото и возвращает ключ, который можно положить
    /// в виш/черновик/рассылку. Сам файл скачивается и сжимается сразу после коммита
    /// апдейта — см. <see cref="FlushAsync"/>.
    /// </summary>
    public Task<string> RegisterAsync(PhotoSize photo)
    {
        var fileId = photo.FileId;

        using var storageContext = MediaStorageContext.Create();
        var mediaItem = storageContext.StoredMedia.FirstOrDefault(m => m.Key == fileId);

        if (mediaItem is null)
        {
            storageContext.StoredMedia.Add(new MediaItemModel()
            {
                Key = fileId,
                TelegramFileId = fileId,
                CreatedAt = DateTime.UtcNow
            });

            storageContext.SaveChanges();
            _logger.Debug("Registered media '{key}' for import", fileId);
        }
        else if (mediaItem.TelegramFileId is null)
        {
            mediaItem.TelegramFileId = fileId;
            storageContext.SaveChanges();
        }

        if (mediaItem?.Data is null)
            MarkForImport(fileId);

        // Миниатюра для ячейки таблицы: соберём после коммита, когда у ключа точно есть хозяин
        EmojiPackService.Instance.QueueThumbnail(fileId);

        return Task.FromResult(fileId);
    }

    /// <summary>Просит сохранить блоб сразу после коммита апдейта.</summary>
    public static void MarkForImport(string key)
    {
        if (!string.IsNullOrEmpty(key) && !_uploadedKeys.Contains(key))
            _uploadedKeys.Add(key);
    }

    /// <summary>Запоминает ключ, который апдейт мог освободить (см. <see cref="FlushAsync"/>).</summary>
    public static void ReleaseLater(IEnumerable<string> keys)
    {
        foreach (var key in keys)
            if (!string.IsNullOrEmpty(key) && !_releasedKeys.Contains(key))
                _releasedKeys.Add(key);
    }

    /// <summary>
    /// Доделывает то, что нельзя делать внутри транзакции апдейта: скачивает и сжимает
    /// только что загруженные картинки и убирает блобы, на которые больше никто не ссылается.
    /// </summary>
    public async Task FlushAsync()
    {
        foreach (var key in TakeQueued(_uploadedKeys))
        {
            var imported = await ImportByKeyAsync(key);

            if (!imported)
                _logger.Warning("Uploaded media '{key}' is stored without a blob yet, it stays a file id only", key);
        }

        foreach (var (key, fileId, width, height) in TakeQueuedUpgrades())
        {
            if (!await UpgradeAsync(key, fileId, width, height))
                _logger.Warning("Failed to upgrade media '{key}' to {width}x{height}, blob stays as is", key, width, height);
        }

        foreach (var key in TakeQueued(_releasedKeys))
            await DeleteIfUnused(key);

        // Миниатюры вишей (стикеры кастомных эмодзи) — тоже только после коммита:
        // сеть внутри транзакции апдейта не трогаем
        await EmojiPackService.Instance.FlushAsync();
    }

    /// <summary>
    /// id миниатюр (кастомных эмодзи) для картинок вишей — одним запросом на страницу списка.
    /// Пустой словарь, если миниатюры выключены: тогда экран придёт с маркерами 🖼️.
    /// </summary>
    public static Dictionary<string, string> GetEmojiIds(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, string>();

        if (!EmojiPackService.Instance.Enabled)
            return result;

        var keyList = keys.Where(key => !string.IsNullOrEmpty(key)).Distinct().ToList();

        if (keyList.Count == 0)
            return result;

        using var storageContext = MediaStorageContext.Create();

        var emojiIds = storageContext.StoredMedia
            .Where(m => m.CustomEmojiId != null && keyList.Contains(m.Key))
            .Select(m => new { m.Key, m.CustomEmojiId })
            .ToList();

        foreach (var item in emojiIds)
            result[item.Key] = item.CustomEmojiId;

        return result;
    }

    private static List<(string Key, string FileId, int Width, int Height)> TakeQueuedUpgrades()
    {
        lock (_upgradeQueue)
        {
            var queued = _upgradeQueue.Select(pair => (pair.Key, pair.Value.FileId, pair.Value.Width, pair.Value.Height)).ToList();
            _upgradeQueue.Clear();
            return queued;
        }
    }

    private static List<string> TakeQueued(List<string> queue)
    {
        lock (queue)
        {
            var queued = queue.Distinct().ToList();
            queue.Clear();
            return queued;
        }
    }

    /// <summary>Скачивает и сжимает одну конкретную картинку.</summary>
    private async Task<bool> ImportByKeyAsync(string key)
    {
        MediaItemModel mediaItem;

        using (var storageContext = MediaStorageContext.Create())
        {
            mediaItem = storageContext.StoredMedia.FirstOrDefault(m => m.Key == key);

            if (mediaItem is null || mediaItem.Data is not null)
                return true;
        }

        if (await ImportAsync(mediaItem))
            return true;

        CountImportFailure(mediaItem.MediaItemId);
        return false;
    }

    /// <summary>
    /// Удаляет блоб, если на ключ больше никто не ссылается: ни виши, ни черновики,
    /// ни рассылки. Вызывается после коммита апдейта, который мог удалить последнего хозяина.
    /// </summary>
    public async Task DeleteIfUnused(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        using (var userContext = UserContext.Create())
        {
            var isUsed = userContext.Wishes.Any(w => w.MediaKey == key)
                || userContext.WishDrafts.Any(d => d.MediaKey == key)
                || userContext.Broadcasts.Any(b => b.MediaKey == key);

            if (isUsed)
                return;
        }

        using var storageContext = MediaStorageContext.Create();
        var mediaItem = storageContext.StoredMedia.FirstOrDefault(m => m.Key == key);

        if (mediaItem is null)
            return;

        // Стикер-миниатюра живёт в наборе пользователя и сам не исчезнет: убираем его
        // до того, как забудем file_id стикера вместе со строкой
        await EmojiPackService.Instance.DeleteStickerAsync(mediaItem);

        storageContext.StoredMedia.Remove(mediaItem);
        storageContext.SaveChanges();

        _logger.Information("Dropped media '{key}': nothing references it anymore", key);
    }

    /// <summary>
    /// Просит перекачать блоб, если Telegram показал размер крупнее того, что у нас лежит.
    /// Легаси-картинки импортированы превью: старый бот хранил file_id самого маленького
    /// размера, а <c>getFile</c> по такому file_id отдаёт именно превью, хотя сам file_id
    /// в сообщении Telegram разворачивает в полную фотографию. Скачивание — после коммита
    /// (<see cref="FlushAsync"/>), внутри транзакции сеть не трогаем.
    /// </summary>
    public void RequestUpgradeIfPreview(string key, string fileId, int width, int height)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(fileId) || width <= 0 || height <= 0)
            return;

        using var storageContext = MediaStorageContext.Create();

        var stored = storageContext.StoredMedia
            .Where(m => m.Key == key)
            .Select(m => new { m.Width, m.Height, HasData = m.Data != null })
            .FirstOrDefault();

        if (stored is null || !stored.HasData)
            return;

        if ((long)width * height <= (long)stored.Width * stored.Height)
            return;

        lock (_upgradeQueue)
            _upgradeQueue[key] = (fileId, width, height);
    }

    /// <summary>
    /// Перекачивает блоб по file_id крупного размера и кэширует этот file_id.
    /// Ставит <see cref="MediaItemModel.SizeChecked"/>: картинка уже в полном размере.
    /// </summary>
    public async Task<bool> UpgradeAsync(string key, string fileId, int width, int height)
    {
        if (_client is null || string.IsNullOrEmpty(fileId))
            return false;

        MediaItemModel mediaItem;

        using (var storageContext = MediaStorageContext.Create())
        {
            mediaItem = storageContext.StoredMedia.FirstOrDefault(m => m.Key == key);

            if (mediaItem is null)
                return false;

            // Уже не мельче — достаточно обновить кэш file_id
            if ((long)mediaItem.Width * mediaItem.Height >= (long)width * height)
            {
                mediaItem.TelegramFileId = fileId;
                mediaItem.SizeChecked = true;
                storageContext.SaveChanges();
                return true;
            }
        }

        var compressed = await DownloadAndCompressAsync(fileId, key);

        if (compressed is null)
            return false;

        using (var storageContext = MediaStorageContext.Create())
        {
            var stored = storageContext.StoredMedia.First(m => m.MediaItemId == mediaItem.MediaItemId);
            var oldWidth = stored.Width;
            var oldHeight = stored.Height;

            stored.Data = compressed.Data;
            stored.Width = compressed.Width;
            stored.Height = compressed.Height;
            stored.SizeBytes = compressed.Data.Length;
            stored.TelegramFileId = fileId;
            stored.SizeChecked = true;
            stored.ImportAttempts = 0;

            storageContext.SaveChanges();

            // Блоб стал крупнее: миниатюра из превью могла не собраться — пробуем снова
            EmojiPackService.Instance.QueueThumbnail(key);

            _logger.Information("Upgraded media '{key}': {oldWidth}x{oldHeight} → {width}x{height}, {size} bytes",
                key, oldWidth, oldHeight, compressed.Width, compressed.Height, compressed.Data.Length);
        }

        return true;
    }

    /// <summary>
    /// Полный размер картинки: отправляет её в канал-хранилище по file_id и читает размеры
    /// из ответа (другого способа узнать file_id крупного размера нет: <c>getFile</c>
    /// по file_id превью отдаёт именно превью). Сообщение сразу удаляется.
    /// null — канал не настроен или Telegram не дал отправить.
    /// </summary>
    public async Task<(string FileId, int Width, int Height)?> ResolveLargestSizeAsync(string fileId)
    {
        // Канал может быть не настроен (в конфиге пустышка вроде -1): тогда Telegram
        // откажет в отправке, а мы вернём null и проход по блобам просто остановится
        if (_client is null || string.IsNullOrEmpty(fileId))
            return null;

        try
        {
            var message = await _client.SendPhoto(chatId: _storageChannelId, photo: InputFile.FromFileId(fileId));
            var largest = message.Photo?.Largest();

            try
            {
                await _client.DeleteMessage(chatId: _storageChannelId, messageId: message.MessageId);
            }
            catch (Exception e)
            {
                _logger.Debug("Failed to delete size-probe message [{messageId}]: {error}", message.MessageId, e.Message);
            }

            return largest is null ? null : (largest.FileId, largest.Width, largest.Height);
        }
        catch (Exception e)
        {
            _logger.Warning("Failed to resolve full size of media '{fileId}': {error}", fileId, e.Message);
            return null;
        }
    }

    /// <summary>Скачивает файл из Telegram и сжимает; null — не вышло (или это не картинка).</summary>
    private async Task<CompressedImage> DownloadAndCompressAsync(string fileId, string key)
    {
        try
        {
            var file = await _client.GetFile(fileId);

            if (file.FilePath is null)
            {
                _logger.Warning("Telegram returned no path for media '{key}'", key);
                return null;
            }

            using var stream = new MemoryStream();
            await _client.DownloadFile(file.FilePath, stream);

            var compressed = ImageCompressor.Compress(stream.ToArray());

            if (compressed is null)
                _logger.Warning("Media '{key}' is not a picture we can re-encode, skipping", key);

            return compressed;
        }
        catch (Exception e)
        {
            _logger.Warning("Failed to import media '{key}': {error}", key, e.Message);
            return null;
        }
    }

    /// <summary>Есть ли блоб для ключа (нужно, чтобы решить, можно ли перезалить картинку).</summary>
    public bool HasData(string key)
    {
        using var storageContext = MediaStorageContext.Create();

        return storageContext.StoredMedia.Any(m => m.Key == key && m.Data != null);
    }

    /// <summary>
    /// Что отправить в Telegram для этого ключа: кэш file_id, если он есть, иначе блоб.
    /// null — картинки нет (импорт не удался или ключ пустой).
    /// </summary>
    public Task<PreparedPhoto> PrepareAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return Task.FromResult<PreparedPhoto>(null);

        using var storageContext = MediaStorageContext.Create();

        // Блоб намеренно не читаем: он нужен только если Telegram отвергнет file_id
        var mediaItem = storageContext.StoredMedia
            .Where(m => m.Key == key)
            .Select(m => new { m.TelegramFileId, HasData = m.Data != null })
            .FirstOrDefault();

        if (mediaItem is null)
        {
            // Легаси: ключ — это telegram file_id, записи в медиа-базе ещё нет
            return Task.FromResult(PreparedPhoto.FromFileId(key, key));
        }

        if (mediaItem.TelegramFileId is not null)
            return Task.FromResult(PreparedPhoto.FromFileId(key, mediaItem.TelegramFileId));

        if (!mediaItem.HasData)
        {
            _logger.Warning("Media '{key}' has neither file id nor data, sending without photo", key);
            return Task.FromResult<PreparedPhoto>(null);
        }

        return Task.FromResult(PreparedPhoto.FromData(key, GetData(key)));
    }

    /// <summary>Блоб картинки — для повторной заливки, когда Telegram отверг file_id.</summary>
    public byte[] GetData(string key)
    {
        using var storageContext = MediaStorageContext.Create();

        return storageContext.StoredMedia
            .Where(m => m.Key == key)
            .Select(m => m.Data)
            .FirstOrDefault();
    }

    /// <summary>Запоминает file_id, который Telegram выдал после заливки блоба.</summary>
    public Task RememberFileIdAsync(string key, string fileId)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(fileId))
            return Task.CompletedTask;

        using var storageContext = MediaStorageContext.Create();
        var mediaItem = storageContext.StoredMedia.FirstOrDefault(m => m.Key == key);

        if (mediaItem is null)
        {
            storageContext.StoredMedia.Add(new MediaItemModel()
            {
                Key = key,
                TelegramFileId = fileId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            mediaItem.TelegramFileId = fileId;
        }

        storageContext.SaveChanges();
        _logger.Debug("Cached file id for media '{key}'", key);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Скачивает картинку из Telegram, сжимает и кладёт блобом в базу.
    /// Возвращает false, если файл уже недоступен (тогда попытки считаются в <see cref="MediaItemModel.ImportAttempts"/>).
    /// </summary>
    public async Task<bool> ImportAsync(MediaItemModel mediaItem)
    {
        if (_client is null)
            return false;

        var fileId = mediaItem.TelegramFileId ?? mediaItem.Key;

        var compressed = await DownloadAndCompressAsync(fileId, mediaItem.Key);

        if (compressed is null)
            return false;

        using var storageContext = MediaStorageContext.Create();
        var stored = storageContext.StoredMedia.First(m => m.MediaItemId == mediaItem.MediaItemId);

        stored.Data = compressed.Data;
        stored.Width = compressed.Width;
        stored.Height = compressed.Height;
        stored.SizeBytes = compressed.Data.Length;
        stored.TelegramFileId = fileId;
        stored.ImportAttempts = 0;

        storageContext.SaveChanges();

        _logger.Information("Imported media '{key}': {width}x{height}, {size} bytes",
            mediaItem.Key, compressed.Width, compressed.Height, compressed.Data.Length);

        return true;
    }

    /// <summary>Считает неудачную попытку импорта, чтобы не долбить мёртвый file_id вечно.</summary>
    public void CountImportFailure(int mediaItemId)
    {
        using var storageContext = MediaStorageContext.Create();
        var stored = storageContext.StoredMedia.FirstOrDefault(m => m.MediaItemId == mediaItemId);

        if (stored is null)
            return;

        ++stored.ImportAttempts;
        storageContext.SaveChanges();
    }

    /// <summary>
    /// Пробует убрать сообщение из старого канала-хранилища: картинка уже лежит у нас блобом.
    ///
    /// Возвращает false, если Telegram удалять не дал — обычно потому, что бот не админ
    /// с правом «удалять сообщения» в канале, либо сообщению больше 48 часов. Тогда
    /// <see cref="MediaItemModel.LegacyMessageId"/> остаётся: по нему видно, что в канале
    /// всё ещё лежит копия, и её можно убрать руками.
    /// </summary>
    public async Task<bool> TryDeleteLegacyChannelMessageAsync(int mediaItemId, int messageId)
    {
        if (_client is null)
            return false;

        try
        {
            await _client.DeleteMessage(chatId: _storageChannelId, messageId: messageId);
        }
        catch (Exception e)
        {
            _logger.Debug("Failed to delete legacy media message [{messageId}]: {error}", messageId, e.Message);
            return false;
        }

        using var storageContext = MediaStorageContext.Create();
        var stored = storageContext.StoredMedia.FirstOrDefault(m => m.MediaItemId == mediaItemId);

        if (stored is null)
            return true;

        stored.LegacyMessageId = null;
        storageContext.SaveChanges();

        return true;
    }

    /// <summary>
    /// Заводит записи для картинок, на которые ссылается пользовательская база, но которых
    /// ещё нет в медиа-базе (виши, добавленные до переезда на блобы).
    /// </summary>
    public Task<int> SeedFromUserDatabaseAsync()
    {
        List<string> keys;

        using (var userContext = UserContext.Create())
        {
            keys = userContext.Wishes
                .Where(w => w.MediaKey != null)
                .Select(w => w.MediaKey)
                .Concat(userContext.WishDrafts.Where(d => d.MediaKey != null).Select(d => d.MediaKey))
                .Concat(userContext.Broadcasts.Where(b => b.MediaKey != null).Select(b => b.MediaKey))
                .Distinct()
                .ToList();
        }

        if (keys.Count == 0)
            return Task.FromResult(0);

        using var storageContext = MediaStorageContext.Create();
        var known = storageContext.StoredMedia.Select(m => m.Key).ToHashSet();
        var added = 0;

        foreach (var key in keys.Where(key => !known.Contains(key)))
        {
            storageContext.StoredMedia.Add(new MediaItemModel()
            {
                Key = key,
                TelegramFileId = key,
                CreatedAt = DateTime.UtcNow
            });

            ++added;
        }

        if (added > 0)
        {
            storageContext.SaveChanges();
            _logger.Information("Seeded {count} media items from the user database", added);
        }

        return Task.FromResult(added);
    }
}
