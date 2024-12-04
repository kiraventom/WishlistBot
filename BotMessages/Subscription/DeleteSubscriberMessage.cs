using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class DeleteSubscriberMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public DeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<MySubscribersQuery>("К моим подписчикам");

      var sender = user;

      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (_usersDb.Values.ContainsKey(userId))
            user = _usersDb.Values[userId];
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      Text.Italic("Вы удалили ")
         .InlineMention(user)
         .Italic(" из списка своих подписчиков.");

      user.Subscriptions.Remove(sender.SubscribeId);
   }
}
