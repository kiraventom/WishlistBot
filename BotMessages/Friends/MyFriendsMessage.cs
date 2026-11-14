using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.Text;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Friends;

[AllowedListPositions(ListPosition.Friend)]
[AllowedTypes(QueryParameterType.SetListPageTo)]
[MessageFor(typeof(MyFriendsQuery))]
public class MyFriendsMessage(ILogger logger) : UserBotMessage(logger)
{
    /// <summary>Колонки таблицы: Друг | ДР | Виши.</summary>
    private const int ColumnsCount = 3;

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(l => l.FriendPage)
            .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Profile)
            .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Wishes)
            .First(u => u.UserId == userId);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var friends = user.GetSortedFriends(today);
        var totalCount = friends.Count;

        if (totalCount == 0)
        {
            const string downArrow = "\u2b07\ufe0f";
            const string link = "\U0001f517";

            Text
                .Bold("У вас пока нет друзей :(")
                .LineBreak()
                .Italic($"Пригласите друзей, прислав им ссылку на свой вишлист {downArrow}");

            Keyboard.AddCopyTextButton($"{link} Ссылка на вишлист", user.GetSubscribeLink());

            Keyboard.NewRow();
        }
        else
        {
            Text.Heading("Ваши друзья");
        }

        var page = TextListMessageUtils.AddListControls<MyFriendsQuery, MainMenuQuery>(
            Keyboard, parameters, totalCount, user.ListPositions.FriendPage, buttonCaption: "Страница",
            itemsPerPage: TextListMessageUtils.TableRowsPerPage);

        if (page.Count != 0)
        {
            Text.Table(table =>
            {
                FriendBirthdayLine.AddColumns(table);

                // Над страницей — сколько друзей осталось до неё, под ней — сколько после
                if (page.PageIndex > 0)
                    TextListMessageUtils.AddRemainingRow(table, RemainingCaption(page.FirstIndex), ColumnsCount);

                for (var i = 0; i < page.Count; ++i)
                    FriendBirthdayLine.AppendRow(table, friends[page.FirstIndex + i]);

                var remaining = totalCount - (page.FirstIndex + page.Count);

                if (remaining > 0)
                    TextListMessageUtils.AddRemainingRow(table, RemainingCaption(remaining), ColumnsCount);
            });
        }

        return Task.CompletedTask;
    }

    private static string RemainingCaption(int count) =>
        TextListMessageUtils.RemainingCaption(count, PluralUtils.Friends(count));
}
