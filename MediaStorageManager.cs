using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Database.MediaStorage;
using WishlistBot.Database.Users;

namespace WishlistBot;

public class MediaStorageManager
{
   private bool _inited;

   private ILogger _logger;
   private long _storageChannelId;
   private ITelegramBotClient _client;
   private MediaStorageDb _database;

   public static MediaStorageManager Instance { get; } = new();

   public void Init(ILogger logger, ITelegramBotClient client, MediaStorageDb mediaStorageDb, long storageChannelId)
   {
      if (_inited)
         return;

      _logger = logger;

      _storageChannelId = storageChannelId;
      _client = client;
      _database = mediaStorageDb;
      _inited = true;
   }

   public async Task Store(string fileId)
   {
      if (_database.Values.ContainsKey(fileId))
         return;

      var photo = InputFile.FromFileId(fileId);
      var message = await _client.SendPhoto(chatId: _storageChannelId, photo: photo);
      _database.Add(fileId, message.MessageId);
      _logger.Debug("Stored media '{fileId}' as message id '{messageId}'", fileId, message.MessageId);
   }

   public async Task Cleanup(UsersDb usersDb)
   {
      // TODO TEMP, support broadcasts
      return;
      _logger.Debug("Media storage cleanup started");

      var storedFileIds = _database.Values.Keys;
      var wishesFileIds = usersDb.Values.Values
         .SelectMany(u => u.Wishes)
         .Select(w => w.FileId)
         .Where(i => i != null);

      var currentWishesFileIds = usersDb.Values.Values
         .Select(u => u.CurrentWish)
         .Where(w => w != null)
         .Select(w => w.FileId)
         .Where(i => i != null);

      var usedFileIds = wishesFileIds.Concat(currentWishesFileIds);
      var unusedFileIds = storedFileIds.Except(usedFileIds);

      var count = 0;
      foreach (var unusedFileId in unusedFileIds)
      {
         ++count;
         await Remove(unusedFileId);
      }

      _logger.Debug("Media storage cleanup removed {count} obsolete entities", count);
   }

   private async Task Remove(string fileId)
   {
      if (!_database.Values.TryGetValue(fileId, out var messageId))
         return;

      try
      {
         await _client.DeleteMessage(chatId: _storageChannelId, messageId: messageId);
      }
      catch (Exception e)
      {
         _logger.Error("Failed to delete media storage message [{messageId}], exception: {exception}", messageId, e.ToString());
      }

      _database.Remove(fileId);
   }
}
