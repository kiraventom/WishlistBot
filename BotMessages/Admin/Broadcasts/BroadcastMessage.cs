using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[AllowedTypes(QueryParameterType.SetBroadcastTo, QueryParameterType.CancelJob)]
[ChildMessage(typeof(BroadcastsMessage))]
[MessageFor(typeof(BroadcastQuery))]
public class BroadcastMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        if (parameters.Pop(QueryParameterType.SetBroadcastTo, out var broadcastId))
        {
            var broadcast = userContext.Broadcasts.AsNoTracking().First(b => b.BroadcastId == broadcastId);

            if (parameters.Pop(QueryParameterType.CancelJob))
            {
                // Отменяем то, что ещё не ушло: строки очереди на эту рассылку
                var queued = userContext.PendingNotifications
                    .Where(pending => pending.Type == PendingNotificationType.Broadcast
                        && pending.Payload == broadcast.BroadcastId.ToString())
                    .ToList();

                userContext.PendingNotifications.RemoveRange(queued);
                userContext.SaveChanges();

                Logger.Information("Cancelled broadcast [{id}], {count} queued messages dropped", broadcast.BroadcastId, queued.Count);
            }

            Text.Bold("Broadcast ")
               .Monospace(broadcast.BroadcastId.ToString());

            if (broadcast.DateTimeSent != null)
            {
                var receivedUsersCount =
                    userContext.Users
                    .Include(c => c.ReceivedBroadcasts.Where(b => b.BroadcastId == broadcast.BroadcastId))
                    .Count(u => u.ReceivedBroadcasts.Any());

                var totalUsersCount = userContext.Users.Count();

                var queuedCount = userContext.PendingNotifications
                    .Count(pending => pending.Type == PendingNotificationType.Broadcast
                        && pending.Payload == broadcast.BroadcastId.ToString());

                if (queuedCount > 0)
                {
                    Text
                       .Bold(" is being sent, ")
                       .Monospace(queuedCount.ToString())
                       .Bold(" users are still waiting");

                    Keyboard
                       .NewRow()
                       .AddButton<BroadcastQuery>("Cancel sending!",
                             new QueryParameter(QueryParameterType.SetBroadcastTo, broadcastId),
                             QueryParameter.CancelJob);
                }
                else
                {
                    Text
                       .Bold(" was sent ")
                       .Date(broadcast.DateTimeSent.Value, "d", broadcast.DateTimeSent.Value.ToString("dd.MM.yyyy"))
                       .Bold(" at ")
                       .Date(broadcast.DateTimeSent.Value, "T", broadcast.DateTimeSent.Value.ToString("HH:mm:ss"))
                       .LineBreak();

                    if (broadcast.Deleted)
                    {
                        Text
                           .Bold("Broadcast was deleted");
                    }
                    else
                    {
                        Text
                           .Monospace(receivedUsersCount.ToString())
                           .Bold(" out of ")
                           .Monospace(totalUsersCount.ToString())
                           .Bold(" users received it");

                        if (receivedUsersCount != totalUsersCount)
                        {
                            Keyboard
                               .NewRow()
                               .AddButton<ConfirmBroadcastQuery>($"Send to {totalUsersCount - receivedUsersCount} users",
                                     new QueryParameter(QueryParameterType.SetBroadcastTo, broadcast.BroadcastId));
                        }
                    }

                    if (receivedUsersCount != 0)
                    {
                        Keyboard
                           .NewRow()
                           .AddButton<ConfirmDeleteBroadcastQuery>($"Delete from {receivedUsersCount} users",
                                 new QueryParameter(QueryParameterType.SetBroadcastTo, broadcast.BroadcastId));
                    }
                }
            }
            else
            {
                Text.Bold(" draft");
                Keyboard
                   .NewRow()
                   .AddButton<ConfirmBroadcastQuery>($"Send to all users",
                         new QueryParameter(QueryParameterType.SetBroadcastTo, broadcast.BroadcastId));
            }

            Text.Media(broadcast.MediaKey);

            Text
               .BlockBreak()
               .Bold("Text: ")
               .LineBreak()
               .Monospace(broadcast.Text);

            Keyboard
               .NewRow()
               .AddButton<BroadcastsQuery>("Back");
        }
        else
        {
            Text.Italic("Create new broadcast");
            Keyboard.AddButton<BroadcastsQuery>("Cancel");
            var user = userContext.Users.First(u => u.UserId == userId);
            user.BotState = BotState.ListenForBroadcast;
        }

        return Task.CompletedTask;
    }
}
