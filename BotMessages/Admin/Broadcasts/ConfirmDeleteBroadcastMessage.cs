using Serilog;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(BroadcastMessage))]
public class ConfirmDeleteBroadcastMessage(ILogger logger, UsersDb usersDb, BroadcastsDb broadcastsDb) : UserBotMessage(logger, usersDb)
{
   protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<DeleteBroadcastQuery>()
         .NewRow()
         .AddButton<BroadcastQuery>("Cancel");

      parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

      var broadcast = broadcastsDb.Values[broadcastId];
      var receiversCount = Users.Count(u => u.ReceivedBroadcasts.Any(b => b.BroadcastId == broadcast.Id));

      Text.Italic("Delete broadcast \"")
         .Monospace(broadcast.GetShortText())
         .Italic("\"")
         .ItalicBold(" for ")
         .ItalicBold(receiversCount.ToString())
         .ItalicBold(" users?");

      return Task.CompletedTask;
   }
}
