using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WishlistBot.Model;
using WishlistBot.Model.User;
using WishlistBot.Text;

namespace WishlistBot.Media;

/// <summary>
/// Аватарки пользователей как кастомные эмодзи — ровно та же механика, что у миниатюр
/// вишей, но набор на человека отдельный и в нём всегда один стикер.
///
/// Почему отдельный набор: аватарка одна и меняется сама по себе, а миниатюры вишей
/// растут и удаляются вместе с вишами. Один стикер в наборе позволяет менять его
/// через <c>replaceStickerInSet</c>, не трогая ничего другого.
///
/// Аватарку бот не получает push-уведомлением, её надо спрашивать: раз в сутки
/// (и на старте) идём <c>getUserProfilePhotos</c> и сравниваем <c>file_unique_id</c>
/// с тем, из которого собран стикер. Если аватарки нет — ничего не рисуем.
/// </summary>
public class AvatarPackService
{
    /// <summary>
    /// Запасной эмодзи стикера. Telegram требует хотя бы один — он же показывается
    /// в уведомлениях и при пересылке от не-Premium пользователя.
    /// </summary>
    public const string FallbackEmoji = "\U0001f464";

    /// <summary>Как часто спрашиваем Telegram про аватарку.</summary>
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

    private bool _inited;
    private ILogger _logger;
    private ITelegramBotClient _client;

    public static AvatarPackService Instance { get; } = new();

    public void Init(ILogger logger, ITelegramBotClient client)
    {
        if (_inited)
            return;

        _logger = logger;
        _client = client;
        _inited = true;
    }

    /// <summary>Имя набора аватарки: <c>a&lt;id в hex&gt;_by_&lt;bot_username&gt;</c>.</summary>
    public static string BuildSetName(long telegramId) =>
        $"a{telegramId:x}_by_{Config.Instance.Username}";

    /// <summary>
    /// Ключ аватарки в медиа-хранилище: по нему экран профиля вставляет картинку блоком,
    /// как в окне виша. Синтетический, к Telegram file_id не привязан.
    /// </summary>
    public static string BuildMediaKey(long telegramId) => $"avatar:{telegramId}";

    /// <summary>Id стикеров-аватарок для экранов: один запрос на страницу списка.</summary>
    public static Dictionary<int, string> GetEmojiIds(IEnumerable<int> userIds)
    {
        var result = new Dictionary<int, string>();

        if (!EmojiPackService.Instance.Enabled)
            return result;

        var ids = userIds.Distinct().ToList();

        if (ids.Count == 0)
            return result;

        using var userContext = UserContext.Create();

        var avatars = userContext.Avatars
            .Where(avatar => avatar.CustomEmojiId != null && ids.Contains(avatar.UserId))
            .Select(avatar => new { avatar.UserId, avatar.CustomEmojiId })
            .ToList();

        foreach (var avatar in avatars)
            result[avatar.UserId] = avatar.CustomEmojiId;

        return result;
    }

    /// <summary>Есть ли у пользователя собранная аватарка (для блока картинки в профиле).</summary>
    public static bool HasAvatar(int userId)
    {
        using var userContext = UserContext.Create();

        return userContext.Avatars.Any(avatar => avatar.UserId == userId && avatar.CustomEmojiId != null);
    }

    /// <summary>
    /// Сверяет аватарки с Telegram: берёт пользователей, которых давно не проверяли,
    /// и обновляет стикеры. Возвращает, сколько проверили.
    /// </summary>
    public async Task<int> SyncAsync(int batchSize)
    {
        if (!_inited || _client is null || !EmojiPackService.Instance.Enabled)
            return 0;

        var cutoff = DateTime.UtcNow - RefreshInterval;

        List<(int UserId, long TelegramId, string FirstName)> users;

        using (var userContext = UserContext.Create())
        {
            users = userContext.Users
                .Include(u => u.Avatar)
                .Where(u => u.Avatar == null || u.Avatar.CheckedAt == null || u.Avatar.CheckedAt < cutoff)
                .OrderBy(u => u.Avatar.CheckedAt ?? DateTime.MinValue)
                .Take(batchSize)
                .Select(u => new { u.UserId, u.TelegramId, u.FirstName })
                .ToList()
                .Select(u => (u.UserId, u.TelegramId, u.FirstName))
                .ToList();
        }

        foreach (var (userId, telegramId, firstName) in users)
            await SyncUserAsync(userId, telegramId, firstName);

        return users.Count;
    }

    private async Task SyncUserAsync(int userId, long telegramId, string firstName)
    {
        UserProfilePhotos photos;

        try
        {
            photos = await _client.GetUserProfilePhotos(telegramId, limit: 1);
        }
        catch (Exception e)
        {
            // Пользователь мог запретить доступ или удалить аккаунт: просто попробуем позже
            _logger?.Warning("Failed to read profile photos of [{userId}]: {error}", userId, e.Message);
            TouchCheckedAt(userId);
            return;
        }

        var photo = photos.Photos?.FirstOrDefault()?.Largest();

        if (photo is null)
        {
            // Аватарки нет — и рисовать нечего: чистим стикер и блоб
            await ClearAsync(userId, telegramId);
            return;
        }

        var stored = GetStored(userId);

        if (stored?.SourceUniqueId == photo.FileUniqueId && !string.IsNullOrEmpty(stored.CustomEmojiId))
        {
            TouchCheckedAt(userId);
            return;
        }

        try
        {
            var data = await DownloadAsync(photo.FileId);

            if (data is null)
            {
                TouchCheckedAt(userId);
                return;
            }

            // Блоб для блока картинки в профиле (как фото виша) — сжатый JPEG
            var blob = ImageCompressor.Compress(data);

            if (blob is null)
            {
                _logger?.Warning("Avatar of [{userId}] is not a picture, skipped", userId);
                TouchCheckedAt(userId);
                return;
            }

            // Миниатюра-эмодзи — из того же блоба (он уже перекодирован и не гигантский)
            var thumbnail = ImageCompressor.CompressEmoji(blob.Data);

            if (thumbnail is null)
            {
                _logger?.Warning("Avatar of [{userId}] is too small for a thumbnail ({width}×{height})",
                    userId, blob.Width, blob.Height);
                TouchCheckedAt(userId);
                return;
            }

            SaveBlob(telegramId, blob.Data);

            var (emojiId, stickerFileId, setName) = await UploadAsync(userId, telegramId, firstName, thumbnail, stored);

            if (emojiId is null)
            {
                TouchCheckedAt(userId);
                return;
            }

            SaveAvatar(userId, setName, emojiId, stickerFileId, photo.FileUniqueId);

            _logger?.Information("Avatar of [{userId}] is made: emoji [{emojiId}] in '{setName}'",
                userId, emojiId, setName);
        }
        catch (Exception e)
        {
            _logger?.Warning("Failed to update avatar of [{userId}]: {error}", userId, e.Message);
            TouchCheckedAt(userId);
        }
    }

    /// <summary>Кладёт или заменяет единственный стикер в наборе аватарки.</summary>
    private async Task<(string EmojiId, string FileId, string SetName)> UploadAsync(
        int userId, long telegramId, string firstName, byte[] thumbnail, WishEmojiAvatarModel stored)
    {
        var setName = stored?.SetName ?? BuildSetName(telegramId);

        using var stream = new MemoryStream(thumbnail);
        var uploaded = await _client.UploadStickerFile(
            userId: telegramId,
            sticker: new InputFileStream(stream, "avatar.webp"),
            stickerFormat: StickerFormat.Static);

        var sticker = new InputSticker(
            InputFile.FromFileId(uploaded.FileId),
            StickerFormat.Static,
            [FallbackEmoji]);

        var existing = await TryGetSetAsync(setName);

        if (existing is null)
        {
            await _client.CreateNewStickerSet(
                userId: telegramId,
                name: setName,
                title: $"Аватарка: {firstName}",
                stickers: [sticker],
                stickerType: StickerType.CustomEmoji);
        }
        else
        {
            // В наборе ровно один стикер — заменяем его. Emoji id при этом меняется,
            // поэтому новый id обязательно сохраняем (проверено живой отправкой:
            // rich-probe/avatar_probe.py).
            var old = existing.Stickers.FirstOrDefault();

            if (old is null)
                await _client.AddStickerToSet(telegramId, setName, sticker);
            else
                await _client.ReplaceStickerInSet(telegramId, setName, old.FileId, sticker);
        }

        var set = await TryGetSetAsync(setName);
        var added = set?.Stickers.FirstOrDefault(s => s.CustomEmojiId is not null);

        if (added is null)
        {
            _logger?.Warning("Telegram did not return the avatar sticker of [{userId}]", userId);
            return (null, null, setName);
        }

        return (added.CustomEmojiId, added.FileId, setName);
    }

    private async Task<byte[]> DownloadAsync(string fileId)
    {
        var file = await _client.GetFile(fileId);

        if (file.FilePath is null)
            return null;

        using var stream = new MemoryStream();
        await _client.DownloadFile(file.FilePath, stream);

        return stream.ToArray();
    }

    /// <summary>Аватарки больше нет: убираем набор и блоб, в списках ничего не рисуем.</summary>
    private async Task ClearAsync(int userId, long telegramId)
    {
        var stored = GetStored(userId);

        if (!string.IsNullOrEmpty(stored?.SetName))
        {
            try
            {
                await _client.DeleteStickerSet(stored.SetName);
            }
            catch (Exception e)
            {
                _logger?.Warning("Failed to delete avatar pack '{setName}': {error}", stored.SetName, e.Message);
            }
        }

        using (var userContext = UserContext.Create())
        {
            var avatar = userContext.Avatars.FirstOrDefault(row => row.UserId == userId);

            // Строка обязана появиться даже у того, у кого аватарки никогда не было:
            // иначе CheckedAt остаётся null и сверка возвращается к нему каждый проход
            if (avatar is null)
            {
                avatar = new WishEmojiAvatarModel() { UserId = userId };
                userContext.Avatars.Add(avatar);
            }

            avatar.SetName = null;
            avatar.CustomEmojiId = null;
            avatar.CustomEmojiFileId = null;
            avatar.SourceUniqueId = null;
            avatar.CheckedAt = DateTime.UtcNow;
            avatar.UpdatedAt = null;

            userContext.SaveChanges();
        }

        using (var storageContext = MediaStorageContext.Create())
        {
            var blob = storageContext.StoredMedia.FirstOrDefault(m => m.Key == BuildMediaKey(telegramId));

            if (blob is not null)
            {
                storageContext.StoredMedia.Remove(blob);
                storageContext.SaveChanges();
            }
        }
    }

    private static WishEmojiAvatarModel GetStored(int userId)
    {
        using var userContext = UserContext.Create();

        return userContext.Avatars.FirstOrDefault(avatar => avatar.UserId == userId);
    }

    private static void TouchCheckedAt(int userId)
    {
        using var userContext = UserContext.Create();

        var avatar = userContext.Avatars.FirstOrDefault(row => row.UserId == userId);

        if (avatar is null)
        {
            avatar = new WishEmojiAvatarModel() { UserId = userId };
            userContext.Avatars.Add(avatar);
        }

        avatar.CheckedAt = DateTime.UtcNow;
        userContext.SaveChanges();
    }

    private static void SaveAvatar(int userId, string setName, string emojiId, string stickerFileId, string sourceUniqueId)
    {
        using var userContext = UserContext.Create();

        var avatar = userContext.Avatars.FirstOrDefault(row => row.UserId == userId);

        if (avatar is null)
        {
            avatar = new WishEmojiAvatarModel() { UserId = userId };
            userContext.Avatars.Add(avatar);
        }

        avatar.SetName = setName;
        avatar.CustomEmojiId = emojiId;
        avatar.CustomEmojiFileId = stickerFileId;
        avatar.SourceUniqueId = sourceUniqueId;
        avatar.CheckedAt = DateTime.UtcNow;
        avatar.UpdatedAt = DateTime.UtcNow;

        userContext.SaveChanges();
    }

    /// <summary>Кладёт картинку аватарки в медиа-базу: профиль покажет её блоком, как фото виша.</summary>
    private static void SaveBlob(long telegramId, byte[] jpeg)
    {
        var key = BuildMediaKey(telegramId);

        using var storageContext = MediaStorageContext.Create();

        var item = storageContext.StoredMedia.FirstOrDefault(m => m.Key == key);

        if (item is null)
        {
            item = new MediaItemModel()
            {
                Key = key,
                CreatedAt = DateTime.UtcNow,
                SizeChecked = true
            };

            storageContext.StoredMedia.Add(item);
        }

        using var data = SkiaSharp.SKData.CreateCopy(jpeg);
        using var image = SkiaSharp.SKImage.FromEncodedData(data);

        item.Data = jpeg;
        item.Width = image?.Width ?? 0;
        item.Height = image?.Height ?? 0;
        item.SizeBytes = jpeg.Length;

        storageContext.SaveChanges();
    }

    private async Task<StickerSet> TryGetSetAsync(string setName)
    {
        try
        {
            return await _client.GetStickerSet(setName);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
