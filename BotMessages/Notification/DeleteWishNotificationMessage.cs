using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Notification;

public class DeleteWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish oldWish) : BotMessage(logger), INotificationMessage
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<SubscriptionQuery>("Перейти к подписке",
                                       QueryParameter.ReadOnly,
                                       new QueryParameter(QueryParameterType.SetUserTo, notificationSource.SenderId))
         .NewRow()
         .AddButton<MainMenuQuery>("В главное меню");

      Text
         .InlineMention(notificationSource)
         .Italic(" удалил виш '")
         .ItalicBold(oldWish.Name)
         .Italic("'!");

      return Task.CompletedTask;
   }
}
