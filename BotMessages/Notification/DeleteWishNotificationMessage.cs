using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class DeleteWishNotificationMessage : BotMessage
{
   private readonly BotUser _notificationSource;
   private readonly Wish _oldWish;

   public DeleteWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish oldWish) : base(logger)
   {
      _notificationSource = notificationSource;
      _oldWish = oldWish;
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      Keyboard
         .AddButton<SubscriptionQuery>("Перейти к подписке", 
               new QueryParameter(QueryParameterType.SetUserTo, _notificationSource.SenderId))
         .AddButton<MainMenuQuery>("В главное меню");

      Text
         .InlineMention(_notificationSource)
         .Italic(" удалил виш '")
         .ItalicBold(_oldWish.Name)
         .Italic("'!");
   }
}
