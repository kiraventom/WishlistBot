using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class NewSubscriberNotificationMessage(ILogger logger, BotUser notificationSource, IEnumerable<BotUser> users) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var subscribers = users
         .Where(u => u.Subscriptions.Contains(notificationSource.SubscribeId))
         .ToList();

      var totalCount = subscribers.Count;

      var subscriberIndex = subscribers.IndexOf(notificationSource);
      var pageIndex = subscriberIndex / ListMessageUtils.ItemsPerPage;

      Keyboard
         .AddButton<SubscriberQuery>("Перейти к подписчику",
                                   QueryParameter.ReadOnly,
                                   new QueryParameter(QueryParameterType.SetUserTo, notificationSource.SenderId),
                                   new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
         .NewRow()
         .AddButton<MainMenuQuery>("В главное меню");

      Text
         .InlineMention(notificationSource)
         .Italic(" подписался на ваш вишлист!");

      return Task.CompletedTask;
   }
}
