using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class DeleteWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish oldWish) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<SubscriptionQuery>("Перейти к подписке", 
               new QueryParameter(QueryParameterType.SetUserTo, notificationSource.SenderId))
         .AddButton<MainMenuQuery>("В главное меню");

      Text
         .InlineMention(notificationSource)
         .Italic(" удалил виш '")
         .ItalicBold(oldWish.Name)
         .Italic("'!");
   }
}
