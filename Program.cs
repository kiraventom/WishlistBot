using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Reflection;
using Telegram.Bot;
using WishlistBot.Listeners;
using WishlistBot.Notification;
using WishlistBot.BotMessages;
using WishlistBot.Actions;
using WishlistBot.Actions.Commands;
using WishlistBot.Queries;
using WishlistBot.Jobs;

namespace WishlistBot;

public static class Program
{
   private const string PROJECT_NAME = "WishlistBot";

   private static async Task Main()
   {
      var projectDirPath = GetProjectDirPath();
      Directory.CreateDirectory(projectDirPath);

      if (!TryLoadConfig(projectDirPath, out var config))
      {
         Console.WriteLine("Couldn't parse config, exiting");
         return;
      }

      var logger = InitLogger(projectDirPath, config.Token);

      logger.Information("===== ENTRY POINT =====");

      if (!TryInitTelegramClient(logger, config.Token, out var client))
      {
         logger.Fatal("Couldn't init {TelegramBotClient}, exiting", nameof(TelegramBotClient));
         return;
      }

      // TODO Ugly
      var mediaStorageManagerInited = await TryInitMediaStorageManager(logger, client, config.StorageChannelId);
      if (!mediaStorageManagerInited)
      {
         logger.Fatal("Couldn't init {MediaStorageManager}, exiting", nameof(MediaStorageManager));
         return;
      }

      NotificationService.Instance.Init(logger, client);

      JobManager.Instance.Init(logger, client);

      var messagesFactory = new MessageFactory(logger);

      var commands = BuildCommands(logger, client);
      var queryActions = BuildQueryActions(logger, client, messagesFactory);
      var actions = commands.Concat(queryActions);

      var listeners = BuildListeners(logger, client);

      var telegramController = new TelegramController(logger, client, actions.ToList(), listeners.ToList());
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
      if (OperatingSystem.IsWindows())
      {
         var appDataDirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         return Path.Combine(appDataDirPath, PROJECT_NAME);
      }

      var homeDirPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      return Path.Combine(homeDirPath, ".config", PROJECT_NAME);
   }

   private static Logger InitLogger(string projectDirPath, string botToken)
   {
      var logsDirPath = Path.Combine(projectDirPath, "logs");
      Directory.CreateDirectory(logsDirPath);
      var logFilePath = Path.Combine(logsDirPath, $"{PROJECT_NAME}.log");

      var logger = new LoggerConfiguration()
         .MinimumLevel.Debug()
         .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
         .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
         .CreateLogger();

      return logger;
   }

   private static bool TryLoadConfig(string projectDirPath, out Config config)
   {
      var configFilePath = Path.Combine(projectDirPath, "config.json");
      config = Config.Load(configFilePath);
      return config is not null;
   }

   private static bool TryInitTelegramClient(ILogger logger, string token, out TelegramBotClient client)
   {
      try
      {
         client = new TelegramBotClient(token);
         return true;
      }
      catch (Exception e)
      {
         client = null;
         logger.Error(e.ToString());
         return false;
      }
   }

   private static async Task<bool> TryInitMediaStorageManager(ILogger logger, TelegramBotClient client, long storageChannelId)
   {
      try
      {
         MediaStorageManager.Instance.Init(logger, client, storageChannelId);
         await MediaStorageManager.Instance.Cleanup();
         return true;
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
         return false;
      }
   }

   private static IEnumerable<UserAction> BuildCommands(ILogger logger, TelegramBotClient client)
   {
      yield return new StartCommand(logger, client);
      yield return new AdminCommand(logger, client);
      yield return new HelpCommand(logger, client);
   }

   private static IEnumerable<UserAction> BuildQueryActions(ILogger logger, TelegramBotClient client, MessageFactory messagesFactory)
   {
      var queryTypes = Assembly.GetExecutingAssembly()
         .GetTypes()
         .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IQuery).IsAssignableFrom(t));

      foreach (var queryType in queryTypes)
      {
         var queryActionType = typeof(QueryAction<>).MakeGenericType(queryType);
         var queryAction = (UserAction)Activator.CreateInstance(queryActionType, logger, client, messagesFactory);
         yield return queryAction;
      }
   }

   private static IEnumerable<IListener> BuildListeners(ILogger logger, TelegramBotClient client)
   {
      yield return new AdminMessagesListener(logger, client);
      yield return new WishMessagesListener(logger, client);
      yield return new ProfileMessagesListener(logger, client);
   }
}
