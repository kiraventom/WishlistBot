using Serilog;
using System.IO;
using System.Text.Json;

namespace WishlistBot;

public class Config
{
   /// <summary>
   /// Telegram bot token. Received from <a href="https://t.me/BotFather">BotFather</a>
   /// </summary>
   public string Token { get; }

   /// <summary>
   /// ID of a channel that will work as media storage. 
   /// Bot will copy all received media to this channel to cache them.
   /// It is required because otherwise Telegram will delete them from servers if user deletes them from dialog.
   /// </summary>
   public long StorageChannelId { get; }

   [System.Text.Json.Serialization.JsonConstructor]
   public Config(string token, long storageChannelId)
   {
      Token = token;
      StorageChannelId = storageChannelId;
   }

   public static Config Load(ILogger logger, string filepath)
   {
      var options = new JsonSerializerOptions()
      {
         WriteIndented = true, AllowTrailingCommas = true
      };

      Config config = null;

      try
      {
         using var configFile = File.OpenRead(filepath);
         config = JsonSerializer.Deserialize<Config>(configFile, options);
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
      }

      return config;
   }
}

