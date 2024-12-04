using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class NewWishNotificationMessage : BotMessage
{
   private readonly BotUser _notificationSource;
   private readonly Wish _newWish;

   public NewWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish newWish) : base(logger)
   {
      _notificationSource = notificationSource;
      _newWish = newWish;
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      var wishIndex = _notificationSource.Wishes.IndexOf(_newWish);

      // TODO Fix
      const int wishesPerPage = 5;
      var pageIndex = wishIndex / wishesPerPage;

      Keyboard
         .AddButton<ShowWishQuery>("Перейти к вишу", 
               new QueryParameter(QueryParameterType.SetCurrentWishTo, wishIndex),
               new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
         .AddButton<MainMenuQuery>("В главное меню");

      Text
         .InlineMention(_notificationSource)
         .Italic(" добавил новый виш '")
         .ItalicBold(_newWish.Name)
         .Italic("'!");
   }
}
