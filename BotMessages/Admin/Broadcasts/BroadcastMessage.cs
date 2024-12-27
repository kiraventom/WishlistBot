using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Broadcasts;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[AllowedTypes(QueryParameterType.SetBroadcastTo)]
[ChildMessage(typeof(BroadcastsMessage))]
public class BroadcastMessage(ILogger logger, UsersDb usersDb, BroadcastsDb broadcastsDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      if (parameters.Pop(QueryParameterType.SetBroadcastTo, out var broadcastId))
      {
         var broadcast = broadcastsDb.Values[broadcastId];
         Text.Bold("Broadcast ")
            .Monospace(broadcast.Id.ToString());

         if (broadcast.IsSent)
         {
            var receivedUsersCount = 
               Users.Count(u => u.ReceivedBroadcasts.Any(b => b.BroadcastId == broadcast.Id));
            var totalUsersCount = Users.Count();

            Text
               .Bold(" was first sent ")
               .Monospace(broadcast.DateTimeSent.ToString("dd/MM/yyyy"))
               .Bold(" at ")
               .Monospace(broadcast.DateTimeSent.ToString("HH:mm:ss"))
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
                           new QueryParameter(QueryParameterType.SetBroadcastTo, broadcast.Id));
               }
            }

            if (receivedUsersCount != 0)
            {
               Keyboard
                  .NewRow()
                  .AddButton<ConfirmDeleteBroadcastQuery>($"Delete from {receivedUsersCount} users",
                        new QueryParameter(QueryParameterType.SetBroadcastTo, broadcast.Id));
            }
         }
         else
         {
            Text.Bold(" draft");
            Keyboard
               .NewRow()
               .AddButton<ConfirmBroadcastQuery>($"Send to all users",
                     new QueryParameter(QueryParameterType.SetBroadcastTo, broadcast.Id));
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
         user.BotState = BotState.ListenForBroadcast;
      }

      return Task.CompletedTask;
   }
}
