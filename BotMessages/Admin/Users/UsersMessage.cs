using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Admin.Users;

[AllowedListPositions(ListPosition.AdminUserPage)]
[AllowedTypes(QueryParameterType.SetListPageTo)]
[MessageFor(typeof(UsersQuery))]
public class UsersMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Heading("Users menu");

        var users = userContext.Users
            .Include(u => u.Wishes)
            .Include(u => u.Friends)
            .AsNoTracking()
            .ToList();

        var sender = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(l => l.AdminUserPage)
            .First(u => u.UserId == userId);

        var page = TextListMessageUtils.AddListControls<UsersQuery, AdminMenuQuery>(
            Keyboard, parameters, users.Count, sender.ListPositions.AdminUserPage);

        if (page.Count == 0)
            return Task.CompletedTask;

        Text.Table(table =>
        {
            table
                .Header("Пользователь", "Виши", "Друзья")
                .Align(TableAlign.Left, TableAlign.Left, TableAlign.Right);

            for (var i = 0; i < page.Count; ++i)
                AddUserRow(table, users[page.FirstIndex + i]);
        });

        return Task.CompletedTask;
    }

    private static void AddUserRow(TableBuilder table, UserModel user)
    {
        table.Row(
            cell => cell.InlineUrl(user.FirstName, DeepLink.BuildAdminUser(user.UserId)),
            cell => cell
                .Verbatim($"{user.Wishes.Count} (")
                .Spoiler(user.Wishes.Count(w => w.HasVisibleClaim).ToString())
                .Verbatim(")"),
            cell => cell.Verbatim(user.Friends.Count.ToString()));
    }
}
