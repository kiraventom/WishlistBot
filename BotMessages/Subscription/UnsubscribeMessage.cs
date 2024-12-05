using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class UnsubscribeMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard.AddButton<MySubscriptionsQuery>("К моим подпискам");

      var sender = user;
      user = GetUser(user, parameters);

      Text.Italic("Вы отписались от вишлиста ")
         .InlineMention(user);

      sender.Subscriptions.Remove(user.SubscribeId);

      return Task.CompletedTask;
   }
}
