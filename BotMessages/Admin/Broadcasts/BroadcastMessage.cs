using Serilog;
using WishlistBot.Jobs;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[AllowedTypes(QueryParameterType.SetBroadcastTo, QueryParameterType.CancelJob)]
[ChildMessage(typeof(BroadcastsMessage))]
public class BroadcastMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        if (parameters.Pop(QueryParameterType.SetBroadcastTo, out var broadcastId))
        {
            var broadcast = userContext.Broadcasts.AsNoTracking().First(b => b.BroadcastId == broadcastId);

            if (parameters.Pop(QueryParameterType.CancelJob))
            {
                JobManager.Instance.StopBroadcastJob(broadcast.BroadcastId);
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

                var isJobActive = JobManager.Instance.IsBroadcastJobActive(broadcast.BroadcastId);
                if (isJobActive)
                {
                    Text
                       .Bold(" has attached job \"")
                       .Monospace(JobManager.Instance.GetActiveBroadcastJobName(broadcast.BroadcastId))
                       .Bold("\"!");

                    Keyboard
                       .NewRow()
                       .AddButton<BroadcastQuery>("Cancel job!",
                             new QueryParameter(QueryParameterType.SetBroadcastTo, broadcastId),
                             QueryParameter.CancelJob);
                }
                else
                {
                    Text
                       .Bold(" was sent ")
                       .Monospace(broadcast.DateTimeSent.Value.ToString("dd/MM/yyyy"))
                       .Bold(" at ")
                       .Monospace(broadcast.DateTimeSent.Value.ToString("HH:mm:ss"))
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

            Text
               .LineBreak()
               .LineBreak()
               .Bold("Text: ")
               .LineBreak()
               .Monospace(broadcast.Text);

            PhotoFileId = broadcast.FileId;

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
