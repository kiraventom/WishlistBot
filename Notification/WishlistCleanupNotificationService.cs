using Microsoft.EntityFrameworkCore;
using WishlistBot.Model.User;

namespace WishlistBot.Notification;

/// <summary>Кому напоминать о чистке вишлиста и сколько вишей там забронировано.</summary>
public sealed record WishlistCleanupReminder(int UserId, int ClaimedWishesCount);

/// <summary>
/// Собирает напоминания о чистке вишлиста: у кого после праздника прошёл срок,
/// в своём вишлисте остались забронированные виши и включены напоминания.
/// </summary>
public static class WishlistCleanupNotificationService
{
    /// <summary>Имя записи в <see cref="ScheduledNotificationModel"/>.</summary>
    public const string StateName = "wishlist_cleanup";

    public static List<WishlistCleanupReminder> BuildReminders(UserContext userContext, DateOnly today)
    {
        var users = userContext.Users
            .Include(u => u.Settings)
            .Include(u => u.Profile)
            .Include(u => u.Wishes)
            .Include(u => u.WishlistCleanupState)
            .Where(u => u.Settings.WishlistCleanupNotifications && u.Settings.ReceiveNotifications)
            .ToList();

        var reminders = new List<WishlistCleanupReminder>();

        foreach (var user in users)
        {
            // Забронированные виши в своём вишлисте — то, что друзья, скорее всего, уже подарили
            var claimedWishesCount = user.Wishes.Count(wish => wish.ClaimerId is not null);

            if (claimedWishesCount == 0)
                continue;

            var state = user.WishlistCleanupState;

            if (!WishlistCleanupSchedule.IsDue(
                    user.Profile?.Birthday,
                    today,
                    state?.LastReminderDate,
                    state?.NextReminderDate))
                continue;

            reminders.Add(new WishlistCleanupReminder(user.UserId, claimedWishesCount));
        }

        return reminders;
    }

    /// <summary>Отмечает, что напоминание отправлено: срок закрыт, отложенное напоминание снято.</summary>
    public static void MarkReminded(UserContext userContext, int userId, DateOnly today)
    {
        var state = GetOrCreateState(userContext, userId);

        state.LastReminderDate = today;
        state.NextReminderDate = null;
    }

    /// <summary>«Напомнить завтра»: отправляем такое же уведомление на следующий день.</summary>
    public static void Snooze(UserContext userContext, int userId, DateOnly today)
    {
        var state = GetOrCreateState(userContext, userId);

        state.NextReminderDate = today.AddDays(1);
    }

    private static WishlistCleanupStateModel GetOrCreateState(UserContext userContext, int userId)
    {
        var state = userContext.WishlistCleanupStates.FirstOrDefault(s => s.UserId == userId);

        if (state is null)
        {
            state = new WishlistCleanupStateModel { UserId = userId };
            userContext.WishlistCleanupStates.Add(state);
        }

        return state;
    }
}
