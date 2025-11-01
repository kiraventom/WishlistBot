using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin.Users;

[AllowedListPositions(ListPosition.AdminUserPage)]
[AllowedTypes(QueryParameterType.SetListPageTo)]
public class UsersMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Bold("Users menu").LineBreak().LineBreak();

        var users = userContext.Users
            .Include(u => u.Wishes)
            .Include(u => u.Profile)
            .Include(u => u.Subscribers)
            .Include(u => u.Subscriptions)
            .AsNoTracking()
            .ToList();

        var sender = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(l => l.AdminUserPage)
            .First(u => u.UserId == userId);

        var totalCount = users.Count;

        TextListMessageUtils.AddListControls<UsersQuery, AdminMenuQuery>(Text, Keyboard, parameters, totalCount, sender.ListPositions.AdminUserPage, itemIndex =>
        {
            var user = users[itemIndex];
            AddUserText(userContext, user, itemIndex);
        });

        return Task.CompletedTask;
    }

    private void AddUserText(UserContext userContext, UserModel user, int itemIndex)
    {
        const string locked = "\U0001f512";
        const string unlocked = "\U0001f513";

        Text.Bold($"{itemIndex + 1}. ");
        Text.InlineUrl(user.FirstName, $"t.me/{Config.Instance.Username}?start=action=adminshowuser_userid={user.UserId}");
        Text.Verbatim($" {(user.Profile.IsPublic ? unlocked : locked)}");
        Text.Verbatim($" W: {user.Wishes.Count} (")
            .Spoiler(user.Wishes.Count(w => w.ClaimerId != null).ToString())
            .Verbatim(")");
        Text.Verbatim($" S: {user.Subscribers.Count}");
        Text.Verbatim($" S to: {user.Subscriptions.Count}");
    }
}
