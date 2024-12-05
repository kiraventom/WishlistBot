using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class UnsubscribeMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<MySubscriptionsQuery>("К моим подпискам");

      user = GetParameterUser(parameters);

      Text.Italic("Вы отписались от вишлиста ")
         .InlineMention(user);

      user.Subscriptions.Remove(user.SubscribeId);
   }
}
