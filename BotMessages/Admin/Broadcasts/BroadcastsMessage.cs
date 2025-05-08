using Serilog;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[AllowedTypes(QueryParameterType.SetListPageTo)]
[ChildMessage(typeof(AdminMenuMessage))]
public class BroadcastsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var broadcasts = userContext.Broadcasts.AsNoTracking().AsEnumerable().Reverse().ToList();
        var totalCount = broadcasts.Count;

        if (broadcasts.Count != 0)
        {
            var sentBroadcastsCount = broadcasts.Count(b => b.DateTimeSent != null);
            var draftsBroadcastsCount = broadcasts.Count - sentBroadcastsCount;

            Text.Bold("Broadcasts:")
               .LineBreak()
               .Monospace(sentBroadcastsCount.ToString())
               .Bold(" sent, ")
               .Monospace(draftsBroadcastsCount.ToString())
               .Bold(" drafts");
        }
        else
        {
            Text.Bold("No broadcasts");
        }

        ListMessageUtils.AddListControls<BroadcastsQuery, AdminMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            const string pencilEmoji = "\u270f\ufe0f ";
            const string envelopeEmoji = "\u2709\ufe0f ";
            const string trashEmoji = "\U0001f5d1\ufe0f ";

            var broadcast = broadcasts[itemIndex];
            var shortText = broadcast.GetShortText();
            var name = broadcast.DateTimeSent != null
             ? envelopeEmoji + shortText
             : pencilEmoji + shortText;

            if (broadcast.Deleted)
                name = name.Insert(0, trashEmoji);

            Keyboard.AddButton<BroadcastQuery>(
             name,
             new QueryParameter(QueryParameterType.SetBroadcastTo, broadcast.BroadcastId),
             new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
        });

        Keyboard
           .NewRow()
           .AddButton<BroadcastQuery>("Add new broadcast");

        return Task.CompletedTask;
    }
}
