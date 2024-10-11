using Serilog;
using Serilog.Events;
using WishlistBot.Users;

namespace WishlistBot;

class Program
{
   private const string PROJECT_NAME = "WishlistBot";

   static async Task Main()
   {
      var projectDirPath = GetProjectDirPath();
      Directory.CreateDirectory(projectDirPath);

      var logger = InitLogger(projectDirPath);

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

      var telegramController = new TelegramController(logger, config.Token, usersDb);
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
}
