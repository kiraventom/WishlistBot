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

   [System.Text.Json.Serialization.JsonConstructor]
   public Config(string token)
   {
      Token = token;
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

