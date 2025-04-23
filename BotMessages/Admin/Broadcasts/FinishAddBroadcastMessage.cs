using Serilog;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;
using WishlistBot.QueryParameters;
using WishlistBot.Model;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

public class FinishAddBroadcastMessage : BotMessage
{
    private readonly BroadcastsDb _broadcastsDb;
    private readonly Broadcast _newBroadcast;
    private readonly BroadcastModel _newBroadcastModel;

    public FinishAddBroadcastMessage(ILogger logger, BroadcastsDb broadcastsDb, Broadcast newBroadcast) : base(logger)
    {
        _broadcastsDb = broadcastsDb;
        _newBroadcast = newBroadcast;
    }

    public FinishAddBroadcastMessage(ILogger logger, BroadcastModel newBroadcastModel) : base(logger)
    {
        _newBroadcastModel = newBroadcastModel;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        userContext.Broadcasts.Add(_newBroadcastModel);
        userContext.SaveChanges();

        Logger.Information("Broadcast [{id}] '{text}' and fileId [{fileId}] is added to database", _newBroadcastModel.BroadcastId, _newBroadcastModel.Text, _newBroadcastModel.FileId);

        Text.Bold("Broadcast created");

        Keyboard
            .AddButton<BroadcastQuery>("To broadcast", new QueryParameter(QueryParameterType.SetBroadcastTo, _newBroadcastModel.BroadcastId))
            .NewRow()
            .AddButton<BroadcastsQuery>("To broadcasts");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        _broadcastsDb.Add(_newBroadcast);
        Logger.Information("Broadcast [{id}] '{text}' and fileId [{fileId}] is added to database", _newBroadcast.Id, _newBroadcast.Text, _newBroadcast.FileId);

        Text.Bold("Broadcast created");

        Keyboard
           .AddButton<BroadcastQuery>("To broadcast", new QueryParameter(QueryParameterType.SetBroadcastTo, _newBroadcast.Id))
           .NewRow()
           .AddButton<BroadcastsQuery>("To broadcasts");

        return Task.CompletedTask;
    }
}
