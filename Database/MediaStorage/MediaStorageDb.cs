using Serilog;

namespace WishlistBot.Database.MediaStorage;

public class MediaStorageDb : Database<string, int>
{
   private const string MediaStorageDatabaseName = "MediaStorage";
   protected override string DatabaseName => MediaStorageDatabaseName;

   private MediaStorageDb(ILogger logger, string filepath, IReadOnlyDictionary<string, int> media)
      : base(logger, filepath, media)
   {
   }

   public void Add(string fileId, int messageId)
   {
      _values[fileId] = messageId;
      Save();
   }

   public void Remove(string fileId)
   {
      if (_values.ContainsKey(fileId))
      {
         _values.Remove(fileId);
         Save();
      }
      else
      {
         _logger.Warning("Attempt to remove non-existent media [{fileId}]", fileId);
      }
   }

   public static MediaStorageDb Load(ILogger logger, string filePath)
   {
      var values = LoadValues(logger, filePath, MediaStorageDatabaseName);
      if (values is null)
         return null;

      return new MediaStorageDb(logger, filePath, values);
   }
}
