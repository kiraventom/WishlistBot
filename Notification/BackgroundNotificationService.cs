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
        if (!BirthdaySchedule.ShouldRun(now, GetLastRunDate()))
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

        SetLastRunDate(today);

        _logger?.Information(
            "Birthday digest for {today}: sent {sent} digest(s), nothing sent if no birthdays", today, sent);

        return sent;
    }

    private async Task RunLoop()
    {
        while (true)
        {
            try
            {
                await RunBirthdayDigestIfDue(DateTime.Now, SendDigest);
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

    private static DateOnly? GetLastRunDate()
    {
        using var userContext = UserContext.Create();

        return userContext.ScheduledNotifications
            .FirstOrDefault(state => state.Name == BirthdayNotificationService.StateName)
            ?.LastRunDate;
    }

    private static void SetLastRunDate(DateOnly date)
    {
        using var userContext = UserContext.Create();

        var state = userContext.ScheduledNotifications
            .FirstOrDefault(scheduled => scheduled.Name == BirthdayNotificationService.StateName);

        if (state is null)
        {
            state = new ScheduledNotificationModel { Name = BirthdayNotificationService.StateName };
            userContext.ScheduledNotifications.Add(state);
        }

        state.LastRunDate = date;
        userContext.SaveChanges();
    }
}
