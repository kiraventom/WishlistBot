using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;

namespace WishlistBot.BotMessages.Subscription;

public class SubscriptionMessage(ILogger logger, UsersDb usersDb) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      var sender = user;

      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (usersDb.Values.TryGetValue(userId, out var user0))
            user = user0;
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

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
   }
}
