using Serilog;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(ConfirmBroadcastMessage))]
[MessageFor(typeof(FinishBroadcastQuery))]
public class FinishBroadcastMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<BroadcastQuery>("Back to broadcast");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcastToSend = userContext.Broadcasts.First(b => b.BroadcastId == broadcastId);
        broadcastToSend.DateTimeSent = DateTime.Now;

        // Кто уже получил — пропускаем: так работает кнопка «Send to N users» у отправленной рассылки
        var recepientIds = userContext.Users
            .Where(user => !user.ReceivedBroadcasts.Any(received => received.BroadcastId == broadcastToSend.BroadcastId))
            .AsNoTracking()
            .Select(user => user.UserId)
            .ToList();

        // Дальше рассылку разносит NotificationDispatchService — как и остальные уведомления,
        // с отложенной доставкой и паузой между получателями
        NotificationService.Instance.EnqueueBroadcast(userContext, broadcastToSend.BroadcastId, recepientIds);

        Logger.Information("Queued broadcast [{id}] for {count} recepients", broadcastId, recepientIds.Count);

        Text.Italic("Broadcast started");

        return Task.CompletedTask;
    }
}
