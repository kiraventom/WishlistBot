using System.Reflection;
using Serilog;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.Queries.Notification;

namespace WishlistBot.BotMessages;

/// <summary>
/// Собирает экран по запросу. Соответствие «запрос → экран» объявлено атрибутом
/// <see cref="MessageForAttribute"/> на самом экране и собирается один раз на старте,
/// поэтому новый экран не требует правок в этом классе.
/// </summary>
public class MessageFactory(ILogger logger)
{
   private static readonly Dictionary<Type, Func<ILogger, BotMessage>> _builders = BuildBuilders();

   public BotMessage Build(IQuery query, UserContext userContext, string queryId)
   {
      if (_builders.TryGetValue(query.GetType(), out var build))
         return build(logger);

      logger.Error("Failed to find message for query [{queryId}]", queryId);
      return new InvalidMessage(logger);
   }

   private static Dictionary<Type, Func<ILogger, BotMessage>> BuildBuilders()
   {
      var builders = new Dictionary<Type, Func<ILogger, BotMessage>>();

      foreach (var messageType in typeof(MessageFactory).Assembly.GetTypes())
      {
         if (messageType is not { IsClass: true, IsAbstract: false } || !messageType.IsSubclassOf(typeof(BotMessage)))
            continue;

         foreach (var queryType in messageType.GetCustomAttributes<MessageForAttribute>().Select(a => a.QueryType))
            builders[queryType] = messageLogger => (BotMessage)Activator.CreateInstance(messageType, messageLogger);
      }

      // Единственный экран, которому нужен не только логгер: у напоминания о чистке
      // кнопка «Напомнить завтра» открывает тот же экран с другим поведением
      builders[typeof(WishlistCleanupSnoozeQuery)] = messageLogger => new WishlistCleanupMessage(messageLogger, snooze: true);

      return builders;
   }
}
