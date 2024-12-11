using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Notification;

public class EditWishNotificationMessage(ILogger logger, BotUser notificationSource, Wish editedWish, WishPropertyType wishPropertyType)
   : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var wishIndex = notificationSource.Wishes.IndexOf(editedWish);
      var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

      Keyboard
         .AddButton<ShowWishQuery>("Перейти к вишу",
                                   new QueryParameter(QueryParameterType.SetUserTo, notificationSource.SenderId),
                                   new QueryParameter(QueryParameterType.SetWishTo, editedWish.Id),
                                   new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
         .NewRow()
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

      return Task.CompletedTask;
   }
}
