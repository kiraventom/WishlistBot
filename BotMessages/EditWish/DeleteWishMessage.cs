using Serilog;
using WishlistBot.Queries;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Database.Users;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetWishTo)]
[ChildMessage(typeof(ConfirmDeleteWishMessage))]
public class DeleteWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override async Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<CompactListQuery>("Назад к моим вишам");

      parameters.Peek(QueryParameterType.SetWishTo, out var wishId);

      var deletedWish = user.Wishes.FirstOrDefault(w => w.Id == wishId);
      if (deletedWish is null)
      {
         Logger.Error("Can't delete wish {id} from user {userId}, not found", wishId, user.SenderId);
      }
      else
      {
         user.Wishes.Remove(deletedWish);
      }

      user.CurrentWish = null;

      Text.Italic("Виш удалён!");

      var deleteWishNotification = new DeleteWishNotificationMessage(Logger, user, deletedWish);
      await NotificationService.Instance.SendToSubscribers(deleteWishNotification, user);
   }
}
