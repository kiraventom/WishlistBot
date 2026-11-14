using Serilog;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Admin.Broadcasts;

[AllowedListPositions(ListPosition.AdminBroadcast)]
[AllowedTypes(QueryParameterType.SetListPageTo)]
[ChildMessage(typeof(AdminMenuMessage))]
[MessageFor(typeof(BroadcastsQuery))]
public class BroadcastsMessage(ILogger logger) : UserBotMessage(logger)
{
    /// <summary>Колонка таблицы: одна — название рассылки ссылкой.</summary>
    private const int ColumnsCount = 1;

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

            Text.Heading("Broadcasts");

            Text.Monospace(sentBroadcastsCount.ToString())
               .Bold(" sent, ")
               .Monospace(draftsBroadcastsCount.ToString())
               .Bold(" drafts");
        }
        else
        {
            Text.Heading("No broadcasts");
        }

        var page = TextListMessageUtils.AddListControls<BroadcastsQuery, AdminMenuQuery>(
            Keyboard, parameters, totalCount, sender.ListPositions.AdminBroadcastPage, buttonCaption: "Страница",
            itemsPerPage: TextListMessageUtils.TableRowsPerPage);

        if (page.Count != 0)
        {
            Text.Table(table =>
            {
                table
                    .Header("Рассылка")
                    .Align(TableAlign.Left);

                // Над страницей — сколько рассылок осталось до неё, под ней — сколько после
                if (page.PageIndex > 0)
                    TextListMessageUtils.AddRemainingRow(table, RemainingCaption(page.FirstIndex), ColumnsCount);

                for (var i = 0; i < page.Count; ++i)
                {
                    var broadcast = broadcasts[page.FirstIndex + i];

                    table.Row(
                        cell => cell.InlineUrl(BroadcastName(broadcast), broadcast.BuildLink()));
                }

                var remaining = totalCount - (page.FirstIndex + page.Count);

                if (remaining > 0)
                    TextListMessageUtils.AddRemainingRow(table, RemainingCaption(remaining), ColumnsCount);
            });
        }

        Keyboard
           .NewRow()
           .AddButton<BroadcastQuery>("Add new broadcast");

        return Task.CompletedTask;
    }

    private static string RemainingCaption(int count) =>
        TextListMessageUtils.RemainingCaption(count, PluralUtils.Broadcasts(count));

    private static string BroadcastName(BroadcastModel broadcast)
    {
        const string pencilEmoji = "\u270f\ufe0f ";
        const string envelopeEmoji = "\u2709\ufe0f ";
        const string trashEmoji = "\U0001f5d1\ufe0f ";

        var name = broadcast.DateTimeSent != null
            ? envelopeEmoji + broadcast.GetShortText()
            : pencilEmoji + broadcast.GetShortText();

        return broadcast.Deleted ? trashEmoji + name : name;
    }
}
