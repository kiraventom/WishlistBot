using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WishlistBot;

[method: JsonConstructor]
public class Config(string token, long storageChannelId, long adminId)
{
   /// <summary>
   /// Telegram bot token. Received from <a href="https://t.me/BotFather">BotFather</a>
   /// </summary>
   public string Token { get; } = token;

   /// <summary>
   /// ID of a channel that will work as media storage. 
   /// Bot will copy all received media to this channel to cache them.
   /// It is required because otherwise Telegram will delete them from servers if user deletes them from dialog.
   /// </summary>
   public long StorageChannelId { get; } = storageChannelId;

   /// <summary>
   /// Only user with this ID will have access to admin commands.
   /// </summary>
   public long AdminId { get; } = adminId;

   public static Config Load(ILogger logger, string filepath)
   {
      Config config = null;

      try
      {
         using var configFile = File.OpenRead(filepath);
         config = JsonSerializer.Deserialize<Config>(configFile, CommonOptions.Json);
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
      }

      return config;
   }
}
