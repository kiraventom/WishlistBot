using Serilog;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(BroadcastMessage))]
public class ConfirmBroadcastMessage(ILogger logger, BroadcastsDb broadcastsDb) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

      var broadcast = broadcastsDb.Values[broadcastId];

      Text.Italic("Are you sure you want to broadcast this?")
         .LineBreak()
         .LineBreak()
         .ExpandableQuote(broadcast.Text);

      PhotoFileId = broadcast.FileId;

      Keyboard
         .AddButton<FinishBroadcastQuery>("Send")
         .NewRow()
         .AddButton<BroadcastQuery>("Cancel");

      return Task.CompletedTask;
   }
}
