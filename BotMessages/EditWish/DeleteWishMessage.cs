using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Database.Users;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetCurrentWishTo)]
public class DeleteWishMessage(ILogger logger) : BotMessage(logger)
{
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<CompactListQuery>("Назад к моим вишам");

      parameters.Peek(QueryParameterType.SetCurrentWishTo, out var wishIndex);

      var deletedWish = user.Wishes[(int)wishIndex];
      user.Wishes.Remove(deletedWish);
      user.CurrentWish = null;

      Text.Italic("Виш удалён!");

      var deleteWishNotification = new DeleteWishNotificationMessage(Logger, user, deletedWish);
      await NotificationService.Instance.Send(deleteWishNotification, user);
   }
}
