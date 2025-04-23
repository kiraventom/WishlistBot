using Serilog;
using Telegram.Bot;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.Jobs;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(ConfirmDeleteBroadcastMessage))]
public class DeleteBroadcastMessage(ILogger logger, UsersDb usersDb, BroadcastsDb broadcastsDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<BroadcastsQuery>("Back to broadcasts");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcastToDelete = userContext.Broadcasts.First(b => b.BroadcastId == broadcastId);
        broadcastToDelete.Deleted = true;

        var recepientIds = userContext.Users.AsNoTracking().Select(c => c.UserId).ToList();
        JobManager.Instance.StartJob("Delete broadcast", broadcastToDelete.BroadcastId, recepientIds, TimeSpan.FromSeconds(1), (BroadcastJobActionDelegate)DeleteBroadcast);

        Text.Italic("Broadcast deletion started");
        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<BroadcastsQuery>("Back to broadcasts");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcastToDelete = broadcastsDb.Values[broadcastId];
        broadcastToDelete.Deleted = true;

        JobManager.Instance.Legacy_StartJob("Delete broadcast", broadcastToDelete, Users, TimeSpan.FromSeconds(1), Legacy_DeleteBroadcast);

        Text.Italic("Broadcast deletion started");
        return Task.CompletedTask;
    }

    private static async Task DeleteBroadcast(ILogger logger, ITelegramBotClient client, UserContext userContext, int userId, int broadcastId)
    {
        var deletedBroadcast = userContext.Broadcasts.AsNoTracking().FirstOrDefault(b => b.BroadcastId == broadcastId);
        var recepient = userContext.Users.Include(c => c.ReceivedBroadcasts).FirstOrDefault(u => u.UserId == userId);

        var receivedBroadcast = recepient.ReceivedBroadcasts.FirstOrDefault(b => b.BroadcastId == deletedBroadcast.BroadcastId);
        var didReceiveBroadcast = receivedBroadcast is not null;
        if (!didReceiveBroadcast)
            return;

        var broadcastMessageId = receivedBroadcast.MessageId;

        try
        {
            await client.DeleteMessage(chatId: recepient.TelegramId, messageId: broadcastMessageId);

            recepient.ReceivedBroadcasts.Remove(receivedBroadcast);
            logger.Information("Deleted broadcast [{bId}] from [{uId}]", deletedBroadcast.BroadcastId, recepient.UserId);
        }
        catch (Exception)
        {
            logger.Information("Failed to delete broadcast message [{messageId}], looks like it was sent more than 48 hours ago", broadcastMessageId);
        }
    }

    private static async Task Legacy_DeleteBroadcast(ILogger logger, ITelegramBotClient client, UsersDb usersDb, BotUser recepient, Broadcast deletedBroadcast)
    {
        var didReceiveBroadcast = recepient.ReceivedBroadcasts.Any(b => b.BroadcastId == deletedBroadcast.Id);
        if (!didReceiveBroadcast)
            return;

        var receivedBroadcast = recepient.ReceivedBroadcasts.First(b => b.BroadcastId == deletedBroadcast.Id);
        var broadcastMessageId = receivedBroadcast.MessageId;

        try
        {
            await client.DeleteMessage(chatId: recepient.SenderId, messageId: broadcastMessageId);

            recepient.ReceivedBroadcasts.Remove(receivedBroadcast);
            logger.Information("Deleted broadcast [{bId}] from [{uId}]", deletedBroadcast.Id, recepient.SenderId);
        }
        catch (Exception)
        {
            logger.Information("Failed to delete broadcast message [{messageId}], looks like it was sent more than 48 hours ago", broadcastMessageId);
        }
    }
}
