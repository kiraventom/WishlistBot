using Serilog;
using WishlistBot.Queries;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.SetWishTo)]
[ChildMessage(typeof(ConfirmDeleteWishMessage))]
public class DeleteWishMessage(ILogger logger) : BotMessage(logger)
{
    protected override async Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<CompactListQuery>("Назад к моим вишам");
        
        parameters.Peek(QueryParameterType.SetWishTo, out var wishId);

        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .Include(u => u.Wishes)
            .First(u => u.UserId == userId);

        var deletedWish = user.Wishes.FirstOrDefault(w => w.WishId == wishId);
        var deletedWishName = deletedWish?.Name;
        if (deletedWish is null)
        {
            Logger.Error("Can't delete wish {id} from user {userId}, not found", wishId, user.UserId);
            user.CurrentWish = null;
            return;
        }
        else
        {
            DeleteWish(userContext, user, deletedWish);
        }

        user.CurrentWish = null;

        Text.Italic("Виш удалён!");

        var deleteWishNotification = new NotificationModel()
        {
            SourceId = user.UserId,
            Type = NotificationMessageType.WishDelete,
        };

        deleteWishNotification.SetExtraString(deletedWishName);

        await NotificationService.Instance.SendToSubscribers(deleteWishNotification, userContext);
    }
}
