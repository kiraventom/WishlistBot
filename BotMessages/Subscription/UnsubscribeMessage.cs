using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class UnsubscribeMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public UnsubscribeMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<MySubscriptionsQuery>("К моим подпискам");

      BotUser userToUnsubscribeFrom = null;
      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (_usersDb.Values.ContainsKey(userId))
            userToUnsubscribeFrom = _usersDb.Values[userId];
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      Text.Italic("Вы отписались от вишлиста ")
         .InlineMention(userToUnsubscribeFrom);

      user.Subscriptions.Remove(userToUnsubscribeFrom.SubscribeId);
   }
}
