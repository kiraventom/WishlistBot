using System.Text;
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

      var changedItemsNames = new List<string>();

      if (wishPropertyType.HasFlag(WishPropertyType.Name))
         changedItemsNames.Add("название");

      if (wishPropertyType.HasFlag(WishPropertyType.Description))
         changedItemsNames.Add("описание");

      if (wishPropertyType.HasFlag(WishPropertyType.Media))
         changedItemsNames.Add("фото");

      if (wishPropertyType.HasFlag(WishPropertyType.Links))
         changedItemsNames.Add("ссылки");

      string changedItemsText;
      changedItemsText = changedItemsNames.Count switch
      {
         0 => throw new NotSupportedException($"WishPropertyType value '{wishPropertyType}' is not supported"),
         1 => changedItemsNames.Single(),
         _ => string.Join(", ", changedItemsNames.SkipLast(1)) + $" и {changedItemsNames.Last()}",
      };

      Text
         .InlineMention(notificationSource)
         .Italic(" изменил ")
         .ItalicBold(changedItemsText)
         .Italic(" у виша '")
         .ItalicBold(editedWish.Name)
         .Italic("'!");

      return Task.CompletedTask;
   }
}
