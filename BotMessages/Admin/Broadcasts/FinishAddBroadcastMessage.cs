using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

public class FinishAddBroadcastMessage(ILogger logger, BroadcastsDb broadcastsDb, Broadcast newBroadcast) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      broadcastsDb.Add(newBroadcast);
      Logger.Information("Broadcast [{id}] '{text}' and fileId [{fileId}] is added to database", newBroadcast.Id, newBroadcast.Text, newBroadcast.FileId);

      Text.Bold("Broadcast created");

      Keyboard
         .AddButton<BroadcastQuery>("To broadcast", new QueryParameter(QueryParameterType.SetBroadcastTo, newBroadcast.Id))
         .NewRow()
         .AddButton<BroadcastsQuery>("To broadcasts");

      return Task.CompletedTask;
   }
}
