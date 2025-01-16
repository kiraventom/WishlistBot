using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(MySubscriptionsMessage))]
public class SubscriptionMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      user = GetUser(user, parameters);

      Keyboard.AddButton<ConfirmUnsubscribeQuery>("Отписаться");

      if (user.Wishes.Count != 0)
      {
         Keyboard
            .NewRow()
            .AddButton<CompactListQuery>("Открыть вишлист");
      }

      Keyboard
         .NewRow()
         .AddButton<MySubscriptionsQuery>("К моим подпискам");

      Text.Bold("Подписка на ")
         .InlineMention(user)
         .Bold(":")
         .LineBreak()
         .Bold("Вишей в вишлисте: ")
         .Monospace(user.Wishes.Count.ToString());

      return Task.CompletedTask;
   }
}
