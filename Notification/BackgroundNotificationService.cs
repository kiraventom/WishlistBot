using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.BotMessages.Notification;
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
    /// Один тик планировщика. Возвращает число отправленных дайджестов (0 — если
    /// не время или некому отправлять). Отправка вынесена в делегат, чтобы это
    /// можно было тестировать без Telegram.
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
            "Birthday digest for {today}: sent {sent} digest(s), nothing sent if no birthdays", today, sent);

        return sent;
    }

    /// <summary>
    /// Один тик напоминаний о чистке вишлиста. Возвращает число отправленных
    /// напоминаний. Отправка вынесена в делегат, чтобы это можно было тестировать
    /// без Telegram; отметка об отправке пишется в том же контексте.
    /// </summary>
    public async Task<int> RunWishlistCleanupIfDue(DateTime now, Func<UserContext, WishlistCleanupReminder, Task> sendReminder)
    {
        if (!WishlistCleanupSchedule.IsRunTime(now))
            return 0;

        var today = DateOnly.FromDateTime(now);

        if (GetLastRunDate(WishlistCleanupNotificationService.StateName) == today)
            return 0;

        var sent = 0;

        using (var userContext = UserContext.Create())
        {
            foreach (var reminder in WishlistCleanupNotificationService.BuildReminders(userContext, today))
            {
                await sendReminder(userContext, reminder);
                WishlistCleanupNotificationService.MarkReminded(userContext, reminder.UserId, today);
                userContext.SaveChanges();
                ++sent;
            }
        }

        SetLastRunDate(WishlistCleanupNotificationService.StateName, today);

        _logger?.Information(
            "Wishlist cleanup reminder for {today}: sent {sent} reminder(s)", today, sent);

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
            }
            catch (Exception e)
            {
                _logger?.Error(e.ToString());
            }

            await Task.Delay(CheckInterval);
        }
    }

    private Task SendDigest(UserContext userContext, BirthdayDigest digest) =>
        NotificationService.Instance.SendToUser(
            new BirthdayListMessage(_logger, digest.Friends), userContext, digest.UserId);

    private Task SendCleanupReminder(UserContext userContext, WishlistCleanupReminder reminder) =>
        NotificationService.Instance.SendToUser(
            new WishlistCleanupMessage(_logger, reminder.ClaimedWishesCount), userContext, reminder.UserId);

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
