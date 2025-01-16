using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Notification;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetUserTo)]
public class FinishSubscriptionMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var sender = user;
      user = GetUser(user, parameters);

      if (sender.Subscriptions.Contains(user.SubscribeId))
      {
         Text.Italic("Вы уже подписаны на вишлист ")
            .InlineMention(user)
            .Italic(".");
      }
      else
      {
         Text.Italic("Вы успешно подписались на вишлист ")
            .InlineMention(user)
            .Italic("!");

         sender.Subscriptions.Add(user.SubscribeId);

         var newSubscriberNotification = new NewSubscriberNotificationMessage(Logger, sender, Users);
         await NotificationService.Instance.SendToUser(newSubscriberNotification, user);
      }

      Keyboard
         .AddButton<CompactListQuery>($"Открыть вишлист {user.FirstName}",
                                      QueryParameter.ReadOnly,
                                      new QueryParameter(QueryParameterType.SetUserTo, user.SenderId))
         .NewRow()
         .AddButton<MySubscriptionsQuery>("К моим подпискам");
   }
}
