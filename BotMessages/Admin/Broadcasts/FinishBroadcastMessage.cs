using Serilog;
using Telegram.Bot;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.Notification;
using WishlistBot.Jobs;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(ConfirmBroadcastMessage))]
public class FinishBroadcastMessage(ILogger logger, UsersDb usersDb, BroadcastsDb broadcastsDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<BroadcastQuery>("Back to broadcast");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcastToSend = userContext.Broadcasts.First(b => b.BroadcastId == broadcastId);
        broadcastToSend.DateTimeSent = DateTime.Now;

        Logger.Information("Started sending broadcast [{id}]", broadcastId);

        var recepientIds = userContext.Users.AsNoTracking().Select(c => c.UserId).ToList();
        JobManager.Instance.StartJob("Send broadcast", broadcastToSend.BroadcastId, recepientIds, TimeSpan.FromSeconds(1), SendBroadcast);

        Text.Italic("Broadcast started");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<BroadcastQuery>("Back to broadcast");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcastToSend = broadcastsDb.Values[broadcastId];
        broadcastToSend.DateTimeSent = DateTime.Now;

        Logger.Information("Started sending broadcast [{id}]", broadcastId);

        JobManager.Instance.Legacy_StartJob("Send broadcast", broadcastToSend, Users, TimeSpan.FromSeconds(1), Legacy_SendBroadcast);

        Text.Italic("Broadcast started");

        return Task.CompletedTask;
    }

    private static async Task SendBroadcast(ILogger logger, ITelegramBotClient client, UserContext userContext, int recepientId, int broadcastId)
    {
        var broadcastToSend = userContext.Broadcasts.FirstOrDefault(b => b.BroadcastId == broadcastId);
        var recepient = userContext.Users.Include(c => c.ReceivedBroadcasts.Where(b => b.ReceiverId == recepientId)).FirstOrDefault(u => u.UserId == recepientId);

        var receivedBroadcast = recepient.ReceivedBroadcasts.FirstOrDefault(b => b.BroadcastId == broadcastToSend.BroadcastId);
        var didReceiveBroadcast = receivedBroadcast is not null;
        if (!didReceiveBroadcast)
            return;

        try
        {
            var broadcastNotification = new BroadcastNotificationMessage(logger, broadcastToSend);
            var broadcastMessageId = await NotificationService.Instance.BroadcastToUser(broadcastNotification, userContext, recepient);

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

    private static async Task Legacy_SendBroadcast(ILogger logger, ITelegramBotClient client, UsersDb usersDb, BotUser recepient, Broadcast broadcast)
    {
        if (recepient.ReceivedBroadcasts.Any(b => b.BroadcastId == broadcast.Id))
            return;

        try
        {
            var broadcastNotification = new BroadcastNotificationMessage(logger, broadcast);
            var broadcastMessageId = await NotificationService.Instance.Legacy_BroadcastToUser(broadcastNotification, recepient);

            recepient.ReceivedBroadcasts.Add(new ReceivedBroadcast(broadcast.Id, broadcastMessageId));

            logger.Information("Sent broadcast [{bId}] to [{uId}], messageId [{mId}]", broadcast.Id, recepient.SenderId, broadcastMessageId);
        }
        catch
        {
            logger.Error("Failed to sent broadcast to [{uId}]", recepient.SenderId);
        }
    }
}
