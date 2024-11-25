using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Database.MediaStorage;

namespace WishlistBot;

public class MediaStorageManager
{
   private bool _inited;

   private long _storageChannelId;
   private ITelegramBotClient _client;
   private MediaStorageDb _database;

   public static MediaStorageManager Instance { get; } = new MediaStorageManager();

   public void Init(ITelegramBotClient client, MediaStorageDb mediaStorageDb, long storageChannelId)
   {
      if (_inited)
         return;

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
   }

   // TODO Call this somewhere
   public async Task Remove(string fileId)
   {
      if (!_database.Values.ContainsKey(fileId))
         return;

      var messageId = _database.Values[fileId];
      await _client.DeleteMessage(chatId: _storageChannelId, messageId: messageId);
      _database.Remove(fileId);
   }
}
