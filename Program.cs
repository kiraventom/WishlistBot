using Serilog;
using Serilog.Events;
using Telegram.Bot;
using WishlistBot.Database.Users;
using WishlistBot.Database.MediaStorage;
using WishlistBot.Notification;

namespace WishlistBot;

class Program
{
   private const string PROJECT_NAME = "WishlistBot";

   static async Task Main()
   {
      var projectDirPath = GetProjectDirPath();
      Directory.CreateDirectory(projectDirPath);

      var logger = InitLogger(projectDirPath);

      logger.Information("===== ENTRY POINT =====");

      if (!TryLoadConfig(logger, projectDirPath, out var config))
      {
         logger.Fatal("Couldn't parse config, exiting");
         return;
      }

      if (!TryLoadUsersDb(logger, projectDirPath, out var usersDb))
      {
         logger.Fatal("Couldn't parse users DB, exiting");
         return;
      }

      if (!TryLoadMediaStorageDb(logger, projectDirPath, out var mediaStorageDb))
      {
         logger.Fatal("Couldn't parse media storage DB, exiting");
         return;
      }

      var client = new TelegramBotClient(config.Token);
      MediaStorageManager.Instance.Init(logger, client, mediaStorageDb, config.StorageChannelId);
      await MediaStorageManager.Instance.Cleanup(usersDb);

      NotificationService.Instance.Init(logger, client, usersDb);

      var telegramController = new TelegramController(logger, client, usersDb);
      telegramController.StartReceiving();

      while (true)
      {
         if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
            return;

         await Task.Delay(10);
      }
   }

   private static string GetProjectDirPath()
   {
      if (System.OperatingSystem.IsWindows())
      {
         var appDataDirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         return Path.Combine(appDataDirPath, PROJECT_NAME);
      }

      var homeDirPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      return Path.Combine(homeDirPath, $".{PROJECT_NAME}");
   }

   private static ILogger InitLogger(string projectDirPath)
   {
      var logsDirPath = Path.Combine(projectDirPath, "logs");
      Directory.CreateDirectory(logsDirPath);
      var logFilePath = Path.Combine(logsDirPath, $"{PROJECT_NAME}.log");

      var logger = new LoggerConfiguration()
         .MinimumLevel.Debug()
         .WriteTo.File(logFilePath)
         .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
         .CreateLogger();

      return logger;
   }

   private static bool TryLoadConfig(ILogger logger, string projectDirPath, out Config config)
   {
      var configFilePath = Path.Combine(projectDirPath, "config.json");
      config = Config.Load(logger, configFilePath);
      return config is not null;
   }

   private static bool TryLoadUsersDb(ILogger logger, string projectDirPath, out UsersDb usersDb)
   {
      var usersDbFilePath = Path.Combine(projectDirPath, "users.json");
      usersDb = UsersDb.Load(logger, usersDbFilePath);
      return usersDb is not null;
   }

   private static bool TryLoadMediaStorageDb(ILogger logger, string projectDirPath, out MediaStorageDb mediaStorageDb)
   {
      var mediaStorageDbFilePath = Path.Combine(projectDirPath, "mediaStorage.json");
      mediaStorageDb = MediaStorageDb.Load(logger, mediaStorageDbFilePath);
      return mediaStorageDb is not null;
   }
}
