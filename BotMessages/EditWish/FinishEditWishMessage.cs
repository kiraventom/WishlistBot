using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetCurrentWishTo)]
public class FinishEditWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow();

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<CompactListQuery>("Назад к моим вишам");

      if (parameters.Pop(QueryParameterType.SetCurrentWishTo, out var wishIndex))
      {
         var editedWish = user.CurrentWish;

         var wishBeforeEditing = user.Wishes[(int)wishIndex];
         user.Wishes.Remove(wishBeforeEditing);
         user.Wishes.Insert((int)wishIndex, editedWish);
         Text.Italic("Виш изменён!");

         WishPropertyType wishPropertyType;

         if (wishBeforeEditing.Name != editedWish.Name)
            wishPropertyType = WishPropertyType.Name;
         else if (wishBeforeEditing.Description != editedWish.Description)
            wishPropertyType = WishPropertyType.Description;
         else if (!wishBeforeEditing.Links.SequenceEqual(editedWish.Links))
            wishPropertyType = WishPropertyType.Links;
         else if (wishBeforeEditing.FileId != editedWish.FileId)
            wishPropertyType = WishPropertyType.Media;
         else
            wishPropertyType = 0;

         if (wishPropertyType != 0)
         {
            var editWishNotification = new EditWishNotificationMessage(Logger, user, editedWish, wishPropertyType);
            await NotificationService.Instance.Send(editWishNotification, user);
         }
      }
      else
      {
         var newWish = user.CurrentWish;

         user.Wishes.Add(newWish);
         Text.Italic("Виш добавлен!");

         var newWishNotification = new NewWishNotificationMessage(Logger, user, newWish);
         await NotificationService.Instance.Send(newWishNotification, user);
      }

      user.CurrentWish = null;
   }
}
