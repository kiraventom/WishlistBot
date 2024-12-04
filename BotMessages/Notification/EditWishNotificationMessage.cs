using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class EditWishNotificationMessage : BotMessage
{
   private readonly BotUser _notificationSource;
   private readonly Wish _editedWish;
   private readonly WishPropertyType _wishPropertyType;

   public EditWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish editedWish, WishPropertyType wishPropertyType) : base(logger)
   {
      _notificationSource = notificationSource;
      _editedWish = editedWish;
      _wishPropertyType = wishPropertyType;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      var wishIndex = _notificationSource.Wishes.IndexOf(_editedWish);
      // TODO Fix
      const int wishesPerPage = 5;
      var pageIndex = wishIndex / wishesPerPage;

      Keyboard
         .AddButton<ShowWishQuery>("Перейти к вишу", 
               new QueryParameter(QueryParameterType.SetCurrentWishTo, wishIndex),
               new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
         .AddButton<MainMenuQuery>("В главное меню");

      var wishPropertyName = _wishPropertyType switch
      {
         WishPropertyType.Description => "описание",
         WishPropertyType.Media => "фото",
         WishPropertyType.Links => "ссылки",
         WishPropertyType.Name => "название",
         _ => throw new NotSupportedException($"Unexpected value {_wishPropertyType}")
      };

      Text
         .InlineMention(_notificationSource)
         .Italic(" изменил ")
         .ItalicBold(wishPropertyName)
         .Italic(" у виша '")
         .ItalicBold(_editedWish.Name)
         .Italic("'");

      Text.Italic("'!");
   }
}
