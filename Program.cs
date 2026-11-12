using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using WishlistBot.Model;
using WishlistBot.Model.User;
using WishlistBot.Listeners;
using WishlistBot.Media;
using WishlistBot.Notification;
using WishlistBot.BotMessages;
using WishlistBot.Actions;
using WishlistBot.Actions.Commands;
using WishlistBot.Queries;

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

      var logger = InitLogger(projectDirPath);

      logger.Information("===== ENTRY POINT =====");

      if (!TryMigrateDatabases(logger))
      {
         logger.Fatal("Couldn't apply database migrations, exiting");
         return;
      }

      if (!TryInitTelegramClient(logger, config.Token, out var client))
      {
         logger.Fatal("Couldn't init {TelegramBotClient}, exiting", nameof(TelegramBotClient));
         return;
      }

      // TODO Ugly
      var mediaStorageInited = TryInitMediaStorage(logger, client, config.StorageChannelId);
      if (!mediaStorageInited)
      {
         logger.Fatal("Couldn't init {MediaStorage}, exiting", nameof(MediaStorage));
         return;
      }

      // Догружает старые картинки в базу блобами и убирает их из старого канала-хранилища
      MediaImportService.Instance.Start(logger);

      NotificationService.Instance.Init(logger);

      NotificationDispatchService.Instance.Init(logger, client);
      NotificationDispatchService.Instance.Start(logger);

      BackgroundNotificationService.Instance.Start(logger);

      var messagesFactory = new MessageFactory(logger);

      var messageSender = new BotMessageSender(logger, client);

      var commands = BuildCommands(logger, messageSender);
      var queryActions = BuildQueryActions(logger, messageSender, messagesFactory);
      var actions = commands.Concat(queryActions);

      var listeners = BuildListeners(logger, messageSender);

      var telegramController = new TelegramController(logger, client, messageSender, actions.ToList(), listeners.ToList());
      telegramController.StartReceiving();

      while (true)
      {
         if (Console.In.Peek() is (int)'q' or (int)'Q')
            return;

         await Task.Delay(10);
      }
   }

   private static string GetProjectDirPath()
   {
      var curDir = Environment.CurrentDirectory;
      return Path.Combine(curDir, "config");
   }

   private static Logger InitLogger(string projectDirPath)
   {
      var logsDirPath = Path.Combine(projectDirPath, "logs");
      Directory.CreateDirectory(logsDirPath);
      var logFilePath = Path.Combine(logsDirPath, $"{PROJECT_NAME}.log");

      var logger = new LoggerConfiguration()
         .MinimumLevel.Debug()
         .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
         .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
         .CreateLogger();

      Log.Logger = logger;

      return logger;
   }

   private static bool TryLoadConfig(string projectDirPath, out Config config)
   {
      var configFilePath = Path.Combine(projectDirPath, "config.json");
      config = Config.Load(configFilePath);
      return config is not null;
   }

   private static bool TryMigrateDatabases(ILogger logger)
   {
      try
      {
         using var userContext = UserContext.Create();
         Migrate(userContext, logger, "user database");

         using var mediaStorageContext = MediaStorageContext.Create();
         Migrate(mediaStorageContext, logger, "media storage database");

         return true;
      }
      catch (Exception e)
      {
         logger.Fatal(e.ToString());
         return false;
      }
   }

   private static void Migrate(DbContext dbContext, ILogger logger, string description)
   {
      var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

      if (pendingMigrations.Count == 0)
      {
         logger.Information("No pending migrations for the {description}", description);
         return;
      }

      logger.Information(
         "Applying {count} migration(s) to the {description}: {migrations}",
         pendingMigrations.Count,
         description,
         string.Join(", ", pendingMigrations));

      dbContext.Database.Migrate();
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

   private static bool TryInitMediaStorage(ILogger logger, TelegramBotClient client, long storageChannelId)
   {
      try
      {
         MediaStorage.Instance.Init(logger, client, storageChannelId);
         return true;
      }
      catch (Exception e)
      {
         logger.Error(e.ToString());
         return false;
      }
   }

   private static IEnumerable<UserAction> BuildCommands(ILogger logger, BotMessageSender messageSender)
   {
      yield return new StartCommand(logger, messageSender);
      yield return new AdminCommand(logger, messageSender);
      // /help временно выключен: экран HelpMessage остался, команда не регистрируется
   }

   private static IEnumerable<UserAction> BuildQueryActions(ILogger logger, BotMessageSender messageSender, MessageFactory messagesFactory)
   {
      var queryTypes = Assembly.GetExecutingAssembly()
         .GetTypes()
         .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IQuery).IsAssignableFrom(t));

      foreach (var queryType in queryTypes)
      {
         var queryActionType = typeof(QueryAction<>).MakeGenericType(queryType);
         var queryAction = (UserAction)Activator.CreateInstance(queryActionType, logger, messageSender, messagesFactory);
         yield return queryAction;
      }
   }

   private static IEnumerable<IListener> BuildListeners(ILogger logger, BotMessageSender messageSender)
   {
      yield return new AdminMessagesListener(logger, messageSender);
      yield return new WishMessagesListener(logger, messageSender);
      yield return new ProfileMessagesListener(logger, messageSender);
   }
}
