using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

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
      foreach (var claimedWish in user.Wishes.Where(w => w.ClaimerId == sender.SenderId))
      {
         claimedWish.ClaimerId = 0;
      }

      return Task.CompletedTask;
   }
}
