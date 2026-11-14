using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Queries.Admin.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.Admin.Users;

[ChildMessage(typeof(UsersMessage))]
[MessageFor(typeof(UserQuery))]
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
            .Include(u => u.Friends)
            .AsNoTracking();

        var (sender, target) = GetSenderAndTarget(users, userId, parameters);

        Text.Heading($"Пользователь {target.UserId}");

        // Карточка — таблица «поле → значение»: в MarkdownV2 это был список строк
        // с моноширинными значениями, в котором ничего не найти глазами
        Text.Table(table =>
        {
            table
                .Header("Поле", "Значение")
                .Align(TableAlign.Left, TableAlign.Left);

            table.Row("TelegramId", cell => cell.Monospace(target.TelegramId.ToString()).Verbatim(" ").InlineMention(target, "link"));
            table.Row("FirstName", cell => cell.Monospace(target.FirstName));
            table.Row("SubscribeId", cell => cell.Monospace(target.SubscribeId));

            if (target.Tag is not null)
                table.Row("Tag", cell => cell.Monospace(target.Tag));

            table.Row("IsAdmin", cell => cell.Monospace(target.IsAdmin.ToString()));
            table.Row("BotState", cell => cell.Monospace(target.BotState.ToString()));
            table.Row("LastQueryId", cell => cell.Monospace(target.LastQueryId));

            if (target.LastBotMessageId is not null)
                table.Row("LastBotMessageId", cell => cell.Monospace(target.LastBotMessageId.Value.ToString()));

            table.Row("CurrentWish", cell => cell.Monospace(target.CurrentWish?.Name ?? "null"));
            table.Row("Wishes", cell => cell
                .Monospace(target.Wishes.Count.ToString())
                .Verbatim(" (")
                .Spoiler(target.Wishes.Count(w => w.HasVisibleClaim).ToString())
                .Verbatim(" claimed)"));
            table.Row("Friends", cell => cell.Monospace(target.Friends.Count.ToString()));
            table.Row("ClaimedWishes", cell => cell.Monospace(target.ClaimedWishes.Count.ToString()));
            table.Row("Send", cell => cell.Monospace(target.Settings.SendNotifications.ToString()));
            table.Row("Receive", cell => cell.Monospace(target.Settings.ReceiveFriendWishNotifications.ToString()));
            table.Row("Birthday", cell =>
            {
                if (target.Profile.Birthday is not { } birthday)
                    cell.Monospace("null");
                else if (birthday.BirthDate is { } birthDate)
                    cell.Date(birthDate, "d", birthDate.ToString("dd.MM.yyyy"));
                else
                    cell.Monospace($"{birthday} (без года)");
            });
            table.Row("Notes", cell => cell.Monospace(target.Profile.Notes ?? "null"));
        });

        // Длинные технические поля — под спойлер: они нужны редко, но место занимают всегда
        Text.Details("QueryParams и AllowedQueries", body => body
            .Bold("QueryParams: ").Monospace(target.QueryParams).LineBreak()
            .Bold("AllowedQueries: ").Monospace(target.AllowedQueries));

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
