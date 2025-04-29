using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Model;

namespace WishlistBot;

public class MediaStorageManager
{
    private bool _inited;

    private ILogger _logger;
    private long _storageChannelId;
    private ITelegramBotClient _client;

    public static MediaStorageManager Instance { get; } = new();

    public void Init(ILogger logger, ITelegramBotClient client, long storageChannelId)
    {
        if (_inited)
            return;

        _logger = logger;

        _storageChannelId = storageChannelId;
        _client = client;
        _inited = true;
    }

    public async Task Store(string fileId)
    {
        using (var storageContext = MediaStorageContext.Create())
        {
            if (storageContext.StoredMedia.Any(m => m.FileId == fileId))
                return;

            var photo = InputFile.FromFileId(fileId);
            var message = await _client.SendPhoto(chatId: _storageChannelId, photo: photo);
            storageContext.StoredMedia.Add(new MediaItemModel() { FileId = fileId, MessageId = message.MessageId });
            _logger.Debug("Stored media '{fileId}' as message id '{messageId}'", fileId, message.MessageId);

            storageContext.SaveChanges();
        }
    }

    public Task Cleanup()
    {
        // TODO TEMP, support broadcasts
        return Task.CompletedTask;
        /* _logger.Debug("Media storage cleanup started"); */

        /* var storedFileIds = _database.Values.Keys; */
        /* var wishesFileIds = usersDb.Values.Values */
        /*    .SelectMany(u => u.Wishes) */
        /*    .Select(w => w.FileId) */
        /*    .Where(i => i != null); */

        /* var currentWishesFileIds = usersDb.Values.Values */
        /*    .Select(u => u.CurrentWish) */
        /*    .Where(w => w != null) */
        /*    .Select(w => w.FileId) */
        /*    .Where(i => i != null); */

        /* var usedFileIds = wishesFileIds.Concat(currentWishesFileIds); */
        /* var unusedFileIds = storedFileIds.Except(usedFileIds); */

        /* var count = 0; */
        /* foreach (var unusedFileId in unusedFileIds) */
        /* { */
        /*    ++count; */
        /*    await Remove(unusedFileId); */
        /* } */

        /* _logger.Debug("Media storage cleanup removed {count} obsolete entities", count); */
    }

    private async Task Remove(string fileId)
    {
        using (var storageContext = MediaStorageContext.Create())
        {
            var mediaItem = storageContext.StoredMedia.FirstOrDefault(i => i.FileId == fileId);
            if (mediaItem is null)
                return;

            try
            {
                await _client.DeleteMessage(chatId: _storageChannelId, messageId: mediaItem.MessageId);
            }
            catch (Exception e)
            {
                _logger.Error("Failed to delete media storage message [{messageId}], exception: {exception}", mediaItem.MessageId, e.ToString());
            }

            storageContext.StoredMedia.Remove(mediaItem);
            storageContext.SaveChanges();
        }
    }
}
