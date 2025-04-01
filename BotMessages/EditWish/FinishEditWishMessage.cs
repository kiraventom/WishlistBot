using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetWishTo)]
public class FinishEditWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override async Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow();

      if (parameters.Pop(QueryParameterType.SetWishTo, out var wishId))
         await EditWish(user, wishId);
      else
         await AddWish(user);

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.NewRow().AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.NewRow().AddButton<CompactListQuery>("Назад к моим вишам");

      user.CurrentWish = null;
   }

   private async Task AddWish(BotUser user)
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
                                   QueryParameter.ReturnToFullList);

      var newWishNotification = new NewWishNotificationMessage(Logger, user, newWish);
      await NotificationService.Instance.SendToSubscribers(newWishNotification, user);
   }

   private async Task EditWish(BotUser user, long wishId)
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
         Text.Italic("Ваши изменения сохранены, но произошла ошибка. Сообщите разработчику об этом");
         return;
      }

      Text.Italic("Виш изменён!");

      var wishIndex = user.Wishes.IndexOf(wishBeforeEditing);
      user.Wishes.Remove(wishBeforeEditing);
      user.Wishes.Insert(wishIndex, editedWish);

      var wishPropertyType = WishPropertyType.None;

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
         await NotificationService.Instance.SendToSubscribers(editWishNotification, user);
      }
   }
}
