using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model;
using WishlistBot.Queries.Admin.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin.Users;

public class UserMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users
            .Include(u => u.Wishes)
            .Include(u => u.ClaimedWishes)
            .Include(u => u.CurrentWish)
            .Include(u => u.Profile)
            .Include(u => u.Settings)
            .Include(u => u.Subscribers)
            .Include(u => u.Subscriptions)
            .AsNoTracking();

        var (sender, target) = GetSenderAndTarget(users, userId, parameters);

        Text.Bold("UserId: ").Monospace(target.UserId.ToString()).LineBreak();
        Text.Bold("TelegramId: ").Monospace(target.TelegramId.ToString())
            .Verbatim(" ").InlineMention(target, "link")
            .LineBreak();
        Text.Bold("FirstName: ").Monospace(target.FirstName).LineBreak();
        Text.Bold("SubscribeId: ").Monospace(target.SubscribeId).LineBreak();
        
        if (target.Tag is not null)
            Text.Bold("Tag: ").Monospace(target.Tag).LineBreak();

        Text.Bold("IsAdmin: ").Monospace(target.IsAdmin.ToString()).LineBreak();
        Text.Bold("BotState: ").Monospace(target.BotState.ToString()).LineBreak();
        Text.Bold("LastQueryId: ").Monospace(target.LastQueryId).LineBreak();

        if (target.LastBotMessageId is not null)
            Text.Bold("LastBotMessageId: ").Monospace(target.LastBotMessageId.Value.ToString()).LineBreak();

        Text.Bold("QueryParams: ").Monospace(target.QueryParams).LineBreak();
        Text.Bold("AllowedQueries: ").Monospace(target.AllowedQueries).LineBreak();

        // TODO Link to see wish
        Text.Bold("CurrentWish: ").Monospace(target.CurrentWish?.Name ?? "null").LineBreak();
        Text.Bold("Settings: ").LineBreak()
            .Verbatim("\tSend: ").Monospace(target.Settings.SendNotifications.ToString()).LineBreak()
            .Verbatim("\tReceive: ").Monospace(target.Settings.ReceiveNotifications.ToString()).LineBreak();

        Text.Bold("Profile: ").LineBreak()
            .Verbatim("\tBirthday: ").Monospace(target.Profile.Birthday?.ToShortDateString() ?? "null").LineBreak()
            .Verbatim("\tNotes: ").Monospace(target.Profile.Notes ?? "null").LineBreak()
            .Verbatim("\tIsPublic: ").Monospace(target.Profile.IsPublic.ToString()).LineBreak();

        // TODO Link to see lists
        Text.Bold("Wishes: ").Monospace(target.Wishes.Count.ToString())
            .Verbatim(" (").Spoiler(target.Wishes.Count(w => w.ClaimerId != null).ToString()).Verbatim(" claimed)")
            .LineBreak();
        Text.Bold("Subscriptions: ").Monospace(target.Subscriptions.Count.ToString()).LineBreak();
        Text.Bold("Subscribers: ").Monospace(target.Subscribers.Count.ToString()).LineBreak();
        Text.Bold("ClaimedWishes: ").Monospace(target.ClaimedWishes.Count.ToString()).LineBreak();

        var usersList = users.OrderBy(u => u.UserId).ToList();
        var totalCount = usersList.Count;
        var index = usersList.FindIndex(u => u.UserId == target.UserId);
        var prevIndex = index - 1;
        var nextIndex = index + 1;

        if (index > 0)
            Keyboard.AddButton<UserQuery>($"\u2b05\ufe0f {prevIndex + 1}", new QueryParameter(QueryParameterType.UserId, usersList[prevIndex].UserId));

        Keyboard.AddButton<UsersQuery>("Назад");

        if (index < totalCount - 1)
            Keyboard.AddButton<UserQuery>($"{nextIndex + 1} \u27a1\ufe0f", new QueryParameter(QueryParameterType.UserId, usersList[nextIndex].UserId));

        return Task.CompletedTask;
    }
}

