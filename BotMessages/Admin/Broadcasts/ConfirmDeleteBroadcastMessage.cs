using Serilog;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(BroadcastMessage))]
public class ConfirmDeleteBroadcastMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<DeleteBroadcastQuery>()
           .NewRow()
           .AddButton<BroadcastQuery>("Cancel");

        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcast = userContext.Broadcasts.AsNoTracking().First(b => b.BroadcastId == broadcastId);
        var receiversCount = userContext.Users
            .AsNoTracking()
            .Include(c => c.ReceivedBroadcasts)
            .Count(u => u.ReceivedBroadcasts.Any());

        Text.Italic("Delete broadcast \"")
           .Monospace(broadcast.GetShortText())
           .Italic("\"")
           .ItalicBold(" for ")
           .ItalicBold(receiversCount.ToString())
           .ItalicBold(" users?");

        return Task.CompletedTask;
    }
}
