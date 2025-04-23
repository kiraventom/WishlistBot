using Serilog;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[ChildMessage(typeof(BroadcastMessage))]
public class ConfirmBroadcastMessage(ILogger logger, BroadcastsDb broadcastsDb) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        parameters.Peek(QueryParameterType.SetBroadcastTo, out var broadcastId);

        var broadcast = userContext.Broadcasts.AsNoTracking().First(b => b.BroadcastId == broadcastId);

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

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
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
