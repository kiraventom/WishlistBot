using Serilog;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(ConfirmDeleteBroadcastMessage))]
[MessageFor(typeof(DeleteBroadcastQuery))]
public class DeleteBroadcastMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<BroadcastsQuery>("Back to broadcasts");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcastToDelete = userContext.Broadcasts.First(b => b.BroadcastId == broadcastId);
        broadcastToDelete.Deleted = true;

        var recepientIds = userContext.Users.AsNoTracking().Select(c => c.UserId).ToList();

        // Удаление идёт через ту же очередь, что и доставка: по строке на получателя
        NotificationService.Instance.EnqueueBroadcastDelete(userContext, broadcastToDelete.BroadcastId, recepientIds);

        Text.Italic("Broadcast deletion started");
        return Task.CompletedTask;
    }
}
