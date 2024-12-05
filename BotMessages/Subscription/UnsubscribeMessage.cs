using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class UnsubscribeMessage(ILogger logger, UsersDb usersDb) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<MySubscriptionsQuery>("К моим подпискам");

      BotUser userToUnsubscribeFrom = null;
      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (usersDb.Values.TryGetValue(userId, out var user0))
            userToUnsubscribeFrom = user0;
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      Text.Italic("Вы отписались от вишлиста ")
         .InlineMention(userToUnsubscribeFrom);

      user.Subscriptions.Remove(userToUnsubscribeFrom.SubscribeId);
   }
}
