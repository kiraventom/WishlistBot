using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Subscription;
using System.Text;

namespace WishlistBot.BotMessages;

public class SubscriptionMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public SubscriptionMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      var sender = user;

      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (_usersDb.Values.ContainsKey(userId))
            user = _usersDb.Values[userId];
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

      var isSenderSubscribed = sender.Subscriptions.Contains(user.SubscribeId);

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
