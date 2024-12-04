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

public class SubscriberMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public SubscriberMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      var sender = user;

      parameters.Pop(QueryParameterType.ReturnToSubscriber);

      if (parameters.Peek(QueryParameterType.SetUserTo, out var userId))
      {
         if (_usersDb.Values.ContainsKey(userId))
            user = _usersDb.Values[userId];
         else
            Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
      }

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
   }
}
