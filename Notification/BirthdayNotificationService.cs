using Microsoft.EntityFrameworkCore;
using WishlistBot.Model.User;

namespace WishlistBot.Notification;

/// <summary>Кому и какой список друзей с днями рождения отправлять.</summary>
public sealed record BirthdayDigest(int UserId, List<FriendBirthday> Friends);

/// <summary>
/// Собирает дайджесты дней рождения: для каждого пользователя — его друзья,
/// у которых день рождения в ближайшие <see cref="DaysAhead"/> дней.
/// </summary>
public static class BirthdayNotificationService
{
    /// <summary>Имя записи в <see cref="ScheduledNotificationModel"/>.</summary>
    public const string StateName = "birthdays";

    public const int DaysAhead = 30;

    public static List<BirthdayDigest> BuildDigests(UserContext userContext, DateOnly today)
    {
        var users = userContext.Users
            .Include(u => u.Settings)
            .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Profile)
            .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Wishes)
            .Where(u => u.Settings.BirthdayNotifications)
            .ToList();

        var digests = new List<BirthdayDigest>();

        foreach (var user in users)
        {
            var friends = BirthdayUtils.SortByUpcomingBirthday(user.Friends.Select(f => f.Friend), today)
                .Where(friend => friend.DaysUntil is { } daysUntil && daysUntil <= DaysAhead)
                .ToList();

            if (friends.Count != 0)
                digests.Add(new BirthdayDigest(user.UserId, friends));
        }

        return digests;
    }
}
