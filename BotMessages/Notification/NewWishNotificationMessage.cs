using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class NewWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish newWish) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      var wishIndex = notificationSource.Wishes.IndexOf(newWish);

      // TODO Fix
      const int wishesPerPage = 5;
      var pageIndex = wishIndex / wishesPerPage;

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
   }
}
