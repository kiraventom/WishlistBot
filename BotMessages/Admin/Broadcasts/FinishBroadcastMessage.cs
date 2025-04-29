using Serilog;
using Telegram.Bot;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Notification;
using WishlistBot.Jobs;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(ConfirmBroadcastMessage))]
public class FinishBroadcastMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<BroadcastQuery>("Back to broadcast");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcastToSend = userContext.Broadcasts.First(b => b.BroadcastId == broadcastId);
        broadcastToSend.DateTimeSent = DateTime.Now;

        Logger.Information("Started sending broadcast [{id}]", broadcastId);

        var recepientIds = userContext.Users.AsNoTracking().Select(c => c.UserId).ToList();
        JobManager.Instance.StartJob("Send broadcast", broadcastToSend.BroadcastId, recepientIds, TimeSpan.FromSeconds(1), (BroadcastJobActionDelegate)SendBroadcast);

        Text.Italic("Broadcast started");

        return Task.CompletedTask;
    }

    private static async Task SendBroadcast(ILogger logger, ITelegramBotClient client, UserContext userContext, int recepientId, int broadcastId)
    {
        var broadcastToSend = userContext.Broadcasts.FirstOrDefault(b => b.BroadcastId == broadcastId);
        var recepient = userContext.Users.Include(c => c.ReceivedBroadcasts).FirstOrDefault(u => u.UserId == recepientId);

        var didReceiveBroadcast = recepient.ReceivedBroadcasts.Any(rb => rb.BroadcastId == broadcastToSend.BroadcastId);        
        if (didReceiveBroadcast)
            return;

        try
        {
            var broadcastNotification = new BroadcastNotificationMessage(logger, broadcastToSend.BroadcastId);
            var broadcastMessageId = await NotificationService.Instance.BroadcastToUser(broadcastNotification, userContext, recepient.UserId);

            recepient.ReceivedBroadcasts.Add(new ReceivedBroadcastModel()
            {
                BroadcastId = broadcastToSend.BroadcastId,
                MessageId = broadcastMessageId,
            });

            logger.Information("Sent broadcast [{bId}] to [{uId}], messageId [{mId}]", broadcastToSend.BroadcastId, recepient.UserId, broadcastMessageId);
        }
        catch
        {
            logger.Error("Failed to sent broadcast to [{uId}]", recepient.UserId);
        }
    }
}
