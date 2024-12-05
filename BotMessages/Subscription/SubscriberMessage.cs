using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(MySubscribersMessage))]
public class SubscriberMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var sender = user;

      user = GetParameterUser(parameters);

      var isSenderSubscribed = sender.Subscriptions.Contains(user.SubscribeId);

      if (isSenderSubscribed)
      {
         Keyboard.AddButton<ConfirmUnsubscribeQuery>("Отписаться");
      }
      else
      {
         Keyboard.AddButton<FinishSubscriptionQuery>("Подписаться");
      }

      Keyboard.AddButton<ConfirmDeleteSubscriberQuery>();

      if (user.Wishes.Count != 0)
      {
         Keyboard
            .NewRow()
            .AddButton<CompactListQuery>("Открыть вишлист", QueryParameter.ReturnToSubscriber);
      }

      Keyboard
         .NewRow()
         .AddButton<MySubscribersQuery>("К моим подписчикам");

      Text.Bold("Подписчик ")
         .InlineMention(user)
         .Bold(":")
         .LineBreak()
         .Bold("Вишей в вишлисте: ")
         .Monospace(user.Wishes.Count.ToString());

      return Task.CompletedTask;
   }
}
