using Microsoft.EntityFrameworkCore;
using WishlistBot.Model.User;

namespace WishlistBot.Notification;

/// <summary>
/// Собирает получателей напоминания о чистке вишлиста: у кого после праздника
/// прошёл срок, в своём вишлисте остались забронированные виши и включены
/// напоминания о чистке. Учитывается только собственный переключатель этой
/// рассылки — настройки про виши друзей на неё не влияют.
/// </summary>
public static class WishlistCleanupNotificationService
{
    /// <summary>Имя записи в <see cref="ScheduledNotificationModel"/>.</summary>
    public const string StateName = "wishlist_cleanup";

    public static List<int> BuildReminders(UserContext userContext, DateOnly today)
    {
        var users = userContext.Users
            .Include(u => u.Settings)
            .Include(u => u.Profile)
            .Include(u => u.Wishes)
            .Include(u => u.WishlistCleanupState)
            .Where(u => u.Settings.WishlistCleanupNotifications)
            .ToList();

        var reminders = new List<int>();

        foreach (var user in users)
        {
            // Забронированные виши в своём вишлисте — то, что друзья, скорее всего, уже подарили.
            // Пользователю это число не показываем, оно нужно только как признак «есть что чистить».
            if (!user.Wishes.Any(wish => wish.HasVisibleClaim))
                continue;

            var state = user.WishlistCleanupState;

            if (!WishlistCleanupSchedule.IsDue(
                    user.Profile?.Birthday,
                    today,
                    state?.LastReminderDate,
                    state?.NextReminderDate))
                continue;

            reminders.Add(user.UserId);
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

    /// <summary>Отложено ли напоминание на будущее — по этому признаку прячем кнопку.</summary>
    public static bool IsSnoozed(UserContext userContext, int userId)
    {
        var state = userContext.WishlistCleanupStates.FirstOrDefault(s => s.UserId == userId);

        return state?.NextReminderDate is not null;
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
