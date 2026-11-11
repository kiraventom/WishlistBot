using Microsoft.EntityFrameworkCore;
using Serilog;
using Telegram.Bot;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Model.User;

namespace WishlistBot.Notification;

/// <summary>
/// Доставка отложенных уведомлений.
///
/// Уведомление не должно обрывать то, что человек делает в боте, поэтому оно сначала
/// ложится в очередь (<see cref="PendingNotificationModel"/>) и ждёт, пока получатель
/// замолчит на <see cref="IdleMinutes"/> минут. Кто в бота давно не заходил — получает
/// уведомления сразу, на ближайшем тике.
///
/// Накопившиеся за время активности события друзей уходят одним сводным сообщением
/// (<see cref="MergedNotificationsMessage"/>), фоновые рассылки — каждая своим.
/// </summary>
public sealed class NotificationDispatchService
{
    /// <summary>Сколько получатель должен молчать, прежде чем ему уйдут уведомления.</summary>
    public const int IdleMinutes = 30;

    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(IdleMinutes);
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    public static NotificationDispatchService Instance { get; } = new();

    private bool _started;
    private ILogger _logger;
    private ITelegramBotClient _client;

    public void Init(ILogger logger, ITelegramBotClient client)
    {
        _logger = logger;
        _client = client;
    }

    public void Start(ILogger logger)
    {
        if (_started)
            return;

        _started = true;
        _logger = logger;

        logger.Information("Notification dispatch service started, idle threshold is {minutes} min", IdleMinutes);
        _ = Task.Run(RunLoop);
    }

    /// <summary>
    /// Один тик: разослать всё, что накопилось у получателей, которые сейчас молчат.
    /// Возвращает число обслуженных получателей (0 — если все ещё активны).
    /// </summary>
    public async Task<int> DispatchPending(DateTime utcNow)
    {
        using var userContext = UserContext.Create();

        var recepients = userContext.PendingNotifications
            .Select(pending => pending.RecepientId)
            .Distinct()
            .ToList();

        var served = 0;

        foreach (var recepientId in recepients)
        {
            if (!IsIdle(userContext, recepientId, utcNow))
                continue;

            var queued = userContext.PendingNotifications
                .Where(pending => pending.RecepientId == recepientId)
                .OrderBy(pending => pending.PendingNotificationId)
                .ToList();

            if (queued.Count == 0)
                continue;

            try
            {
                await Deliver(userContext, recepientId, queued);
            }
            catch (Exception e)
            {
                // Строки не трогаем: попробуем на следующем тике
                _logger?.Error("Failed to deliver notifications of user [{userId}]: {error}", recepientId, e.Message);
                continue;
            }

            userContext.PendingNotifications.RemoveRange(queued);
            userContext.SaveChanges();

            PruneDeliveredNotifications(userContext, queued);

            ++served;
        }

        return served;
    }

    /// <summary>
    /// Запись-источник (<see cref="NotificationModel"/>) живёт, пока о ней помнит хоть одна
    /// строка очереди: у одного события бывает несколько получателей.
    /// </summary>
    private static void PruneDeliveredNotifications(UserContext userContext, IReadOnlyList<PendingNotificationModel> delivered)
    {
        var notificationIds = delivered
            .Where(item => item.NotificationId is not null)
            .Select(item => item.NotificationId!.Value)
            .Distinct()
            .ToList();

        if (notificationIds.Count == 0)
            return;

        foreach (var notificationId in notificationIds)
        {
            if (userContext.PendingNotifications.Any(item => item.NotificationId == notificationId))
                continue;

            if (userContext.Notifications.FirstOrDefault(row => row.NotificationId == notificationId) is { } notification)
                userContext.Notifications.Remove(notification);
        }

        userContext.SaveChanges();
    }

    private static bool IsIdle(UserContext userContext, int recepientId, DateTime utcNow)
    {
        var lastActivity = userContext.Users
            .Where(user => user.UserId == recepientId)
            .Select(user => user.LastActivityAt)
            .FirstOrDefault();

        return lastActivity is not { } activity || activity <= utcNow - IdleThreshold;
    }

    private async Task Deliver(UserContext userContext, int recepientId, IReadOnlyList<PendingNotificationModel> queued)
    {
        var recepient = userContext.Users
            .Include(user => user.Settings)
            .First(user => user.UserId == recepientId);

        var events = queued.Where(item => item.Type <= PendingNotificationType.WishPermanent).ToList();
        var background = queued.Where(item => item.Type > PendingNotificationType.WishPermanent).ToList();

        var delivered = false;

        if (events.Count != 0 && recepient.Settings.ReceiveFriendWishNotifications)
            delivered |= await DeliverEvents(userContext, recepient, events);

        foreach (var item in background)
            delivered |= await DeliverBackground(userContext, recepient, item);

        if (!delivered)
            return;

        // Следующий экран уйдёт новым сообщением, чтобы в «All» он оказался под уведомлением
        recepient.NotifiedSinceLastMessage = true;
        userContext.SaveChanges();
    }

    private async Task<bool> DeliverEvents(UserContext userContext, UserModel recepient, IReadOnlyList<PendingNotificationModel> events)
    {
        if (events.Count == 1)
        {
            if (BuildEventMessage(userContext, events[0]) is not { } single)
                return false;

            return await Send(userContext, recepient, single);
        }

        var merged = new MergedNotificationsMessage(_logger, events);
        await merged.Init(userContext, recepient);

        if (merged.ItemsCount == 0)
            return false;

        return await Send(userContext, recepient, merged);
    }

    private BotMessage BuildEventMessage(UserContext userContext, PendingNotificationModel pending)
    {
        if (pending.Type == PendingNotificationType.NewFriend)
        {
            if (!int.TryParse(pending.Payload, out var sourceId))
                return null;

            return new NewFriendNotificationMessage(_logger, sourceId);
        }

        var notification = userContext.Notifications
            .FirstOrDefault(n => n.NotificationId == pending.NotificationId);

        if (notification is null)
            return null;

        return notification.Type switch
        {
            NotificationMessageType.NewWish => new NewWishNotificationMessage(_logger, notification),
            NotificationMessageType.WishEdit => new EditWishNotificationMessage(_logger, notification),
            NotificationMessageType.WishDelete => new DeleteWishNotificationMessage(_logger, notification),
            NotificationMessageType.WishPermanent => new WishPermanentNotificationMessage(_logger, notification),
            _ => null,
        };
    }

    private async Task<bool> DeliverBackground(UserContext userContext, UserModel recepient, PendingNotificationModel pending)
    {
        switch (pending.Type)
        {
            case PendingNotificationType.BirthdayDigest:
            {
                if (!recepient.Settings.BirthdayNotifications)
                    return false;

                var friends = BuildBirthdayDigest(userContext, pending);

                if (friends.Count == 0)
                    return false;

                return await Send(userContext, recepient, new BirthdayListMessage(_logger, friends));
            }

            case PendingNotificationType.WishlistCleanup:
                if (!recepient.Settings.WishlistCleanupNotifications)
                    return false;

                return await Send(userContext, recepient, new WishlistCleanupMessage(_logger));

            case PendingNotificationType.WishlistFill:
                if (!recepient.Settings.WishlistFillNotifications)
                    return false;

                if (!int.TryParse(pending.Payload, out var kind))
                    return false;

                return await Send(userContext, recepient, new WishlistFillMessage(_logger, (WishlistHolidayKind)kind));

            default:
                return false;
        }
    }

    /// <summary>
    /// Дайджест восстанавливается по id друзей из очереди: за время ожидания кто-то мог
    /// потерять день рождения из окна, таких просто не показываем.
    /// </summary>
    private static List<FriendBirthday> BuildBirthdayDigest(UserContext userContext, PendingNotificationModel pending)
    {
        if (string.IsNullOrEmpty(pending.Payload))
            return [];

        var friendIds = pending.Payload
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToList();

        if (friendIds.Count == 0)
            return [];

        var friends = userContext.Users
            .Include(user => user.Profile)
            .Include(user => user.Wishes)
            .Where(user => friendIds.Contains(user.UserId))
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Now);

        return BirthdayUtils.SortByUpcomingBirthday(friends, today)
            .Where(friend => friend.DaysUntil is { } daysUntil && daysUntil <= BirthdayNotificationService.DaysAhead)
            .ToList();
    }

    private async Task<bool> Send(UserContext userContext, UserModel recepient, BotMessage message)
    {
        var sent = await _client.SendOrEditBotMessage(_logger, userContext, recepient.UserId, message, forceNewMessage: true);
        return sent is not null;
    }

    private async Task RunLoop()
    {
        while (true)
        {
            try
            {
                await DispatchPending(DateTime.UtcNow);
            }
            catch (Exception e)
            {
                _logger?.Error(e.ToString());
            }

            await Task.Delay(TickInterval);
        }
    }
}
