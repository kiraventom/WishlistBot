using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Reflection;
using Telegram.Bot;
using WishlistBot.Listeners;
using WishlistBot.Database.Users;
using WishlistBot.Database.MediaStorage;
using WishlistBot.Notification;
using WishlistBot.BotMessages;
using WishlistBot.Actions;
using WishlistBot.Actions.Commands;
using WishlistBot.Queries;
using WishlistBot.Database.Admin;
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

      if (!TryLoadBroadcastsDb(logger, projectDirPath, out var broadcastsDb))
      {
         logger.Fatal("Couldn't parse broadcasts DB, exiting");
         return;
      }

      if (!TryInitTelegramClient(logger, config.Token, out var client))
      {
         logger.Fatal("Couldn't init {TelegramBotClient}, exiting", nameof(TelegramBotClient));
         return;
      }

      // TODO Ugly
      var mediaStorageManagerInited = await TryInitMediaStorageManager(logger, client, usersDb, mediaStorageDb, config.StorageChannelId);
      if (!mediaStorageManagerInited)
      {
         logger.Fatal("Couldn't init {MediaStorageManager}, exiting", nameof(MediaStorageManager));
         return;
      }

      NotificationService.Instance.Init(logger, client, usersDb);

      JobManager.Instance.Init(logger, client, usersDb);

      var messagesFactory = new MessageFactory(logger, usersDb, broadcastsDb);

      var commands = BuildCommands(logger, client, usersDb);
      var queryActions = BuildQueryActions(logger, client, messagesFactory);
      var actions = commands.Concat(queryActions);

      var listeners = BuildListeners(logger, client, usersDb, broadcastsDb);

      var telegramController = new TelegramController(logger, client, usersDb, actions.ToList(), listeners.ToList());
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
         .WriteTo.File(logFilePath)
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

   private static bool TryLoadBroadcastsDb(ILogger logger, string projectDirPath, out BroadcastsDb broadcastsDb)
   {
      var broadcastsDbFilePath = Path.Combine(projectDirPath, "broadcasts.json");
      broadcastsDb = BroadcastsDb.Load(logger, broadcastsDbFilePath);
      return broadcastsDb is not null;
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

   private static async Task<bool> TryInitMediaStorageManager(ILogger logger, TelegramBotClient client, UsersDb usersDb, MediaStorageDb mediaStorageDb, long storageChannelId)
   {
      try
      {
         MediaStorageManager.Instance.Init(logger, client, mediaStorageDb, storageChannelId);
         await MediaStorageManager.Instance.Cleanup(usersDb);
         return true;
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
         return false;
      }
   }

   private static IEnumerable<UserAction> BuildCommands(ILogger logger, TelegramBotClient client, UsersDb usersDb)
   {
      yield return new StartCommand(logger, client, usersDb);
      yield return new AdminCommand(logger, client, usersDb);
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

   private static IEnumerable<IListener> BuildListeners(ILogger logger, TelegramBotClient client, UsersDb usersDb, BroadcastsDb broadcastsDb)
   {
      yield return new AdminMessagesListener(logger, client, broadcastsDb);
      yield return new WishMessagesListener(logger, client, usersDb);
   }
}
