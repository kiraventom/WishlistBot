using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetWishTo)]
public class FinishEditWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow();

      if (parameters.Pop(QueryParameterType.SetWishTo, out var wishId))
      {
         var editedWish = user.CurrentWish;
         var wishBeforeEditing = user.Wishes.FirstOrDefault(w => w.Id == wishId);
         if (wishBeforeEditing is null)
         {
            Logger.Error("Can't find wish {id} to remove after editing", wishId);

            if (editedWish is null)
            {
               Logger.Error("[{uId}]: Edited wish is null", user.SenderId);
               throw new NotSupportedException("Edited wish is null");
            }

            user.Wishes.Add(editedWish);
         }
         else
         {
            var wishIndex = user.Wishes.IndexOf(wishBeforeEditing);
            user.Wishes.Remove(wishBeforeEditing);
            user.Wishes.Insert(wishIndex, editedWish);
         }

         Text.Italic("Виш изменён!");

         WishPropertyType wishPropertyType = WishPropertyType.None;

         if (wishBeforeEditing.Name != editedWish.Name)
            wishPropertyType |= WishPropertyType.Name;

         if (wishBeforeEditing.Description != editedWish.Description)
            wishPropertyType |= WishPropertyType.Description;

         if (!wishBeforeEditing.Links.SequenceEqual(editedWish.Links))
            wishPropertyType |= WishPropertyType.Links;

         if (wishBeforeEditing.FileId != editedWish.FileId)
            wishPropertyType |= WishPropertyType.Media;

         if (wishPropertyType != WishPropertyType.None)
         {
            var editWishNotification = new EditWishNotificationMessage(Logger, user, editedWish, wishPropertyType);
            await NotificationService.Instance.Send(editWishNotification, user);
         }
      }
      else
      {
         var newWish = user.CurrentWish;

         if (newWish is null)
         {
            Logger.Error("[{uId}]: New wish is null", user.SenderId);
            throw new NotSupportedException("Current wish is null");
         }

         user.Wishes.Add(newWish);
         Text.Italic("Виш добавлен!");

         var wishIndex = user.Wishes.IndexOf(newWish);
         var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

         Keyboard
            .AddButton<EditWishQuery>("Изменить виш",
               new QueryParameter(QueryParameterType.SetWishTo, newWish.Id),
               new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
               QueryParameter.ReturnToFullList)
            .NewRow();

         var newWishNotification = new NewWishNotificationMessage(Logger, user, newWish);
         await NotificationService.Instance.Send(newWishNotification, user);
      }

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<CompactListQuery>("Назад к моим вишам");

      user.CurrentWish = null;
   }
}
