using Serilog;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[AllowedListPositions(ListPosition.AdminBroadcast)]
[AllowedTypes(QueryParameterType.SetListPageTo)]
[ChildMessage(typeof(AdminMenuMessage))]
[MessageFor(typeof(BroadcastsQuery))]
public class BroadcastsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(l => l.AdminBroadcastPage)
            .First(u => u.UserId == userId);

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

        if (totalCount != 0)
            Text.LineBreak().LineBreak();

        TextListMessageUtils.AddListControls<BroadcastsQuery, AdminMenuQuery>(Text, Keyboard, parameters, totalCount, sender.ListPositions.AdminBroadcastPage, itemIndex =>
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

            Text.Bold($"{itemIndex + 1}. ");
            Text.InlineUrl(name, broadcast.BuildLink());
        }, buttonCaption: "Страница");

        Keyboard
           .NewRow()
           .AddButton<BroadcastQuery>("Add new broadcast");

        return Task.CompletedTask;
    }
}
