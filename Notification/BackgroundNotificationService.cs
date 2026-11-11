using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model.User;

namespace WishlistBot.Notification;

/// <summary>
/// Фоновые рассылки по расписанию. Пока в ней одна рассылка — дайджест дней рождения;
/// чтобы добавить новую, достаточно завести своё имя состояния и свой <c>…Schedule</c>.
/// </summary>
public sealed class BackgroundNotificationService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    public static BackgroundNotificationService Instance { get; } = new();

    private bool _started;
    private ILogger _logger;

    public void Start(ILogger logger)
    {
        if (_started)
            return;

        _started = true;
        _logger = logger;

        logger.Information("Background notification service started");
        _ = Task.Run(RunLoop);
    }

    /// <summary>
    /// Один тик планировщика. Возвращает число поставленных в очередь дайджестов
    /// (0 — если не время или некому отправлять). Постановка вынесена в делегат,
    /// чтобы это можно было тестировать без Telegram.
    /// </summary>
    public async Task<int> RunBirthdayDigestIfDue(DateTime now, Func<UserContext, BirthdayDigest, Task> sendDigest)
    {
        if (!BirthdaySchedule.ShouldRun(now, GetLastRunDate(BirthdayNotificationService.StateName)))
            return 0;

        var today = DateOnly.FromDateTime(now);
        var sent = 0;

        using (var userContext = UserContext.Create())
        {
            foreach (var digest in BirthdayNotificationService.BuildDigests(userContext, today))
            {
                await sendDigest(userContext, digest);
                ++sent;
            }
        }

        SetLastRunDate(BirthdayNotificationService.StateName, today);

        _logger?.Information(
            "Birthday digest for {today}: queued {sent} digest(s), nothing queued if no birthdays", today, sent);

        return sent;
    }

    /// <summary>
    /// Один тик напоминаний о чистке вишлиста. Возвращает число отправленных
    /// напоминаний. Отправка вынесена в делегат, чтобы это можно было тестировать
    /// без Telegram; отметка об отправке пишется в том же контексте.
    /// </summary>
    public async Task<int> RunWishlistCleanupIfDue(DateTime now, Func<UserContext, int, Task> sendReminder)
    {
        if (!WishlistCleanupSchedule.IsRunTime(now))
            return 0;

        var today = DateOnly.FromDateTime(now);

        if (GetLastRunDate(WishlistCleanupNotificationService.StateName) == today)
            return 0;

        var sent = 0;

        using (var userContext = UserContext.Create())
        {
            foreach (var userId in WishlistCleanupNotificationService.BuildReminders(userContext, today))
            {
                await sendReminder(userContext, userId);
                WishlistCleanupNotificationService.MarkReminded(userContext, userId, today);
                userContext.SaveChanges();
                ++sent;
            }
        }

        SetLastRunDate(WishlistCleanupNotificationService.StateName, today);

        _logger?.Information(
            "Wishlist cleanup reminder for {today}: queued {sent} reminder(s)", today, sent);

        return sent;
    }

    /// <summary>
    /// Один тик напоминаний заполнить вишлист перед праздником. Возвращает число
    /// отправленных напоминаний; отправка вынесена в делегат для тестов.
    /// </summary>
    public async Task<int> RunWishlistFillIfDue(DateTime now, Func<UserContext, WishlistFillReminder, Task> sendReminder)
    {
        if (!WishlistFillSchedule.IsRunTime(now))
            return 0;

        var today = DateOnly.FromDateTime(now);

        if (GetLastRunDate(WishlistFillNotificationService.StateName) == today)
            return 0;

        var sent = 0;

        using (var userContext = UserContext.Create())
        {
            foreach (var reminder in WishlistFillNotificationService.BuildReminders(userContext, today))
            {
                await sendReminder(userContext, reminder);
                WishlistFillNotificationService.MarkReminded(userContext, reminder.UserId, today);
                userContext.SaveChanges();
                ++sent;
            }
        }

        SetLastRunDate(WishlistFillNotificationService.StateName, today);

        _logger?.Information(
            "Wishlist fill reminder for {today}: queued {sent} reminder(s)", today, sent);

        return sent;
    }

    private async Task RunLoop()
    {
        while (true)
        {
            try
            {
                await RunBirthdayDigestIfDue(DateTime.Now, SendDigest);
                await RunWishlistCleanupIfDue(DateTime.Now, SendCleanupReminder);
                await RunWishlistFillIfDue(DateTime.Now, SendFillReminder);
            }
            catch (Exception e)
            {
                _logger?.Error(e.ToString());
            }

            await Task.Delay(CheckInterval);
        }
    }

    // Рассылки только ставят уведомления в очередь: отправку делает
    // NotificationDispatchService, когда получатель освободится.

    private Task SendDigest(UserContext userContext, BirthdayDigest digest)
    {
        NotificationService.Instance.EnqueueBirthdayDigest(
            userContext, digest.UserId, digest.Friends.Select(friend => friend.Friend.UserId));

        return Task.CompletedTask;
    }

    private Task SendCleanupReminder(UserContext userContext, int userId)
    {
        NotificationService.Instance.EnqueueWishlistCleanup(userContext, userId);
        return Task.CompletedTask;
    }

    private Task SendFillReminder(UserContext userContext, WishlistFillReminder reminder)
    {
        NotificationService.Instance.EnqueueWishlistFill(userContext, reminder.UserId, reminder.Holiday.Kind);
        return Task.CompletedTask;
    }

    private static DateOnly? GetLastRunDate(string stateName)
    {
        using var userContext = UserContext.Create();

        return userContext.ScheduledNotifications
            .FirstOrDefault(state => state.Name == stateName)
            ?.LastRunDate;
    }

    private static void SetLastRunDate(string stateName, DateOnly date)
    {
        using var userContext = UserContext.Create();

        var state = userContext.ScheduledNotifications
            .FirstOrDefault(scheduled => scheduled.Name == stateName);

        if (state is null)
        {
            state = new ScheduledNotificationModel { Name = stateName };
            userContext.ScheduledNotifications.Add(state);
        }

        state.LastRunDate = date;
        userContext.SaveChanges();
    }
}
