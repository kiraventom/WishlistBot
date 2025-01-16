using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Notification;

public class NewSubscriberNotificationMessage(ILogger logger, BotUser notificationSource, IEnumerable<BotUser> users) : BotMessage(logger), INotificationMessage
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var subscribers = users
         .Where(u => u.Subscriptions.Contains(notificationSource.SubscribeId))
         .ToList();

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
