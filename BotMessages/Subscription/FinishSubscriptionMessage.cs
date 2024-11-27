using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class FinishSubscriptionMessage : BotMessage
{
   private readonly BotUser _userToSubscribeTo;

   public FinishSubscriptionMessage(ILogger logger, BotUser userToSubscribeTo) : base(logger)
   {
      _userToSubscribeTo = userToSubscribeTo;
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (_userToSubscribeTo is not null)
      {
         if (user.Subscriptions.Contains(_userToSubscribeTo.SubscribeId))
         {
            Text.Italic("Вы уже подписаны на вишлист ")
               .InlineMention(_userToSubscribeTo.FirstName, _userToSubscribeTo.SenderId)
               .Italic(".");
         }
         else
         {
            Text.Italic("Вы успешно подписались на вишлист ")
               .InlineMention(_userToSubscribeTo.FirstName, _userToSubscribeTo.SenderId)
               .Italic("!");

            user.Subscriptions.Add(_userToSubscribeTo.SubscribeId);
         }

         Keyboard.AddButton<CompactListQuery>($"Открыть вишлист {_userToSubscribeTo.FirstName}", 
               QueryParameter.ReadOnly, 
               new QueryParameter(QueryParameterType.SetUserTo, _userToSubscribeTo.SenderId));
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
