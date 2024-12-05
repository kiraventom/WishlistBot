using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class NewWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish newWish) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var wishIndex = notificationSource.Wishes.IndexOf(newWish);

      var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

      Keyboard
         .AddButton<ShowWishQuery>("Перейти к вишу",
                                   new QueryParameter(QueryParameterType.SetCurrentWishTo, wishIndex),
                                   new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
         .AddButton<MainMenuQuery>("В главное меню");

      Text
         .InlineMention(notificationSource)
         .Italic(" добавил новый виш '")
         .ItalicBold(newWish.Name)
         .Italic("'!");

      return Task.CompletedTask;
   }
}
