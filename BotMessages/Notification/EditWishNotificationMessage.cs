using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class EditWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish editedWish, WishPropertyType wishPropertyType)
   : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var wishIndex = notificationSource.Wishes.IndexOf(editedWish);
      // TODO Fix
      const int wishesPerPage = 5;
      var pageIndex = wishIndex / wishesPerPage;

      Keyboard
         .AddButton<ShowWishQuery>("Перейти к вишу", 
               new QueryParameter(QueryParameterType.SetUserTo, notificationSource.SenderId),
               new QueryParameter(QueryParameterType.SetCurrentWishTo, wishIndex),
               new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
         .AddButton<MainMenuQuery>("В главное меню");

      var wishPropertyName = wishPropertyType switch
      {
         WishPropertyType.Description => "описание",
         WishPropertyType.Media => "фото",
         WishPropertyType.Links => "ссылки",
         WishPropertyType.Name => "название",
         _ => throw new NotSupportedException($"Unexpected value {wishPropertyType}")
      };

      Text
         .InlineMention(notificationSource)
         .Italic(" изменил ")
         .ItalicBold(wishPropertyName)
         .Italic(" у виша '")
         .ItalicBold(editedWish.Name)
         .Italic("'!");
   }
}
