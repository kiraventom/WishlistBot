using Serilog;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Friends;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Notification;

/// <summary>
/// Дайджест дней рождения друзей — приходит 1 и 15 числа, одной таблицей.
/// </summary>
public class BirthdayListMessage(ILogger logger, IReadOnlyList<FriendBirthday> friends) : BotMessage(logger), INotificationMessage
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Heading("Дни рождения друзей в ближайшие 30 дней");

        Text.Table(table =>
        {
            FriendBirthdayLine.AddColumns(table);

            for (var i = 0; i < friends.Count; ++i)
                FriendBirthdayLine.AppendRow(table, i + 1, friends[i]);
        });

        Text.AddNotificationsSettingsLink();

        Keyboard
            .AddButton<MyFriendsQuery>("К друзьям")
            .NewRow()
            .AddButton<MainMenuQuery>("В главное меню");

        return Task.CompletedTask;
    }
}
