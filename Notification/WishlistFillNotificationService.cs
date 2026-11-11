using Microsoft.EntityFrameworkCore;
using WishlistBot.Model.User;

namespace WishlistBot.Notification;

/// <summary>Кому напоминать заполнить вишлист и по какому поводу.</summary>
public sealed record WishlistFillReminder(int UserId, WishlistHoliday Holiday);

/// <summary>
/// Собирает напоминания заполнить вишлист: у кого ближайший праздник попал в окно
/// и включён именно этот переключатель уведомлений.
/// </summary>
public static class WishlistFillNotificationService
{
    /// <summary>Имя записи в <see cref="ScheduledNotificationModel"/>.</summary>
    public const string StateName = "wishlist_fill";

    public static List<WishlistFillReminder> BuildReminders(UserContext userContext, DateOnly today)
    {
        var users = userContext.Users
            .Include(u => u.Settings)
            .Include(u => u.Profile)
            .Include(u => u.WishlistFillState)
            .Where(u => u.Settings.WishlistFillNotifications)
            .ToList();

        var reminders = new List<WishlistFillReminder>();

        foreach (var user in users)
        {
            if (WishlistFillSchedule.GetDueHoliday(user.Profile?.Birthday, today) is not { } holiday)
                continue;

            // Один раз за окно и не чаще раза в 30 дней — даже если прошлое напоминание
            // было про другой праздник
            if (!WishlistFillSchedule.IsDue(
                    user.Profile?.Birthday,
                    today,
                    user.WishlistFillState?.LastReminderDate))
                continue;

            reminders.Add(new WishlistFillReminder(user.UserId, holiday));
        }

        return reminders;
    }

    /// <summary>Отмечает, когда напомнили в последний раз.</summary>
    public static void MarkReminded(UserContext userContext, int userId, DateOnly today)
    {
        var state = userContext.WishlistFillStates.FirstOrDefault(s => s.UserId == userId);

        if (state is null)
        {
            state = new WishlistFillStateModel { UserId = userId };
            userContext.WishlistFillStates.Add(state);
        }

        state.LastReminderDate = today;
    }
}
