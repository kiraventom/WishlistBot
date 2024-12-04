using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class FinishSubscriptionMessage : BotMessage
{
   private readonly BotUser _userToSubscribeTo;
   private readonly UsersDb _usersDb;

   public FinishSubscriptionMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

   public FinishSubscriptionMessage(ILogger logger, BotUser userToSubscribeTo) : base(logger)
   {
      _userToSubscribeTo = userToSubscribeTo;
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      BotUser userToSubscribeTo = _userToSubscribeTo;

      if (userToSubscribeTo is null)
      {
         if (parameters.Pop(QueryParameterType.SetUserTo, out var userId))
         {
            if (_usersDb.Values.ContainsKey(userId))
               userToSubscribeTo = _usersDb.Values[userId];
            else
               Logger.Error("Can't set user to [{userId}], users db does not contain user with this ID", userId);
         }
      }

      if (userToSubscribeTo is not null)
      {
         if (user.Subscriptions.Contains(userToSubscribeTo.SubscribeId))
         {
            Text.Italic("Вы уже подписаны на вишлист ")
               .InlineMention(userToSubscribeTo)
               .Italic(".");
         }
         else
         {
            Text.Italic("Вы успешно подписались на вишлист ")
               .InlineMention(userToSubscribeTo)
               .Italic("!");

            user.Subscriptions.Add(userToSubscribeTo.SubscribeId);
         }

         Keyboard.AddButton<CompactListQuery>($"Открыть вишлист {userToSubscribeTo.FirstName}", 
               QueryParameter.ReadOnly, 
               new QueryParameter(QueryParameterType.SetUserTo, userToSubscribeTo.SenderId));
      }
      else
      {
         Text.Italic("Некорректная ссылка, пользователь не найден :(");
      }

      Keyboard
         .NewRow()
         .AddButton<MySubscriptionsQuery>("К моим подпискам");
   }
}
