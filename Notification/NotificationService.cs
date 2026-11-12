using System.Globalization;
using Serilog;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Notification;

/// <summary>
/// Постановка уведомлений в очередь.
///
/// Здесь ничего не отправляется: уведомление кладётся в <see cref="PendingNotificationModel"/>
/// и ждёт, пока получатель освободится — рассылкой занимается
/// <see cref="NotificationDispatchService"/>.
/// </summary>
public class NotificationService
{
    private bool _inited;

    private ILogger _logger;

    public static NotificationService Instance { get; } = new();

    public void Init(ILogger logger)
    {
        if (_inited)
            return;

        _logger = logger;
        _inited = true;
    }

    public Task SendToFriends(NotificationModel notification, UserContext userContext)
    {
        var notificationSource = userContext.Users
            .Include(c => c.Friends)
            .Include(c => c.Settings)
            .First(u => u.UserId == notification.SourceId);

        if (!notificationSource.Settings.SendNotifications)
            return Task.CompletedTask;

        var recepients = notificationSource.Friends.Select(f => f.FriendId).ToList();

        EnqueueNotification(userContext, notification, recepients);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Уведомление об изменении/удалении виша — только забронировавшему.
    /// Если брони нет (или виш постоянный), не отправляем никому и не создаём запись в очереди.
    /// </summary>
    public Task SendToClaimer(NotificationModel notification, UserContext userContext, int? recepientId)
    {
        if (recepientId is not { } claimerId)
            return Task.CompletedTask;

        var notificationSource = userContext.Users
            .Include(c => c.Settings)
            .First(u => u.UserId == notification.SourceId);

        if (!notificationSource.Settings.SendNotifications)
            return Task.CompletedTask;

        EnqueueNotification(userContext, notification, [claimerId]);
        return Task.CompletedTask;
    }

    /// <summary>«Добавил вас в друзья» — единственное событие без записи в <see cref="NotificationModel"/>.</summary>
    public Task SendNewFriend(UserContext userContext, UserModel source, int recepientId)
    {
        EnqueuePending(userContext, recepientId, PendingNotificationType.NewFriend, payload: source.UserId.ToString(CultureInfo.InvariantCulture));
        return Task.CompletedTask;
    }

    /// <summary>Дайджест дней рождения: в очереди лежат id друзей, из которых он собирается.</summary>
    public void EnqueueBirthdayDigest(UserContext userContext, int recepientId, IEnumerable<int> friendIds) =>
        EnqueuePending(userContext, recepientId, PendingNotificationType.BirthdayDigest,
            payload: string.Join(',', friendIds));

    public void EnqueueWishlistCleanup(UserContext userContext, int recepientId) =>
        EnqueuePending(userContext, recepientId, PendingNotificationType.WishlistCleanup);

    public void EnqueueWishlistFill(UserContext userContext, int recepientId, WishlistHolidayKind kind) =>
        EnqueuePending(userContext, recepientId, PendingNotificationType.WishlistFill,
            payload: ((int)kind).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Админская рассылка: по строке очереди на каждого получателя. Дальше её разносит
    /// <see cref="NotificationDispatchService"/> — с той же отложенной доставкой, что и
    /// остальные уведомления.
    /// </summary>
    public void EnqueueBroadcast(UserContext userContext, int broadcastId, IReadOnlyCollection<int> recepientIds) =>
        EnqueueBroadcastAction(userContext, PendingNotificationType.Broadcast, broadcastId, recepientIds);

    /// <summary>Убрать уже доставленную рассылку из чатов получателей.</summary>
    public void EnqueueBroadcastDelete(UserContext userContext, int broadcastId, IReadOnlyCollection<int> recepientIds) =>
        EnqueueBroadcastAction(userContext, PendingNotificationType.BroadcastDelete, broadcastId, recepientIds);

    private static void EnqueueBroadcastAction(UserContext userContext, PendingNotificationType type, int broadcastId, IReadOnlyCollection<int> recepientIds)
    {
        if (recepientIds.Count == 0)
            return;

        var payload = broadcastId.ToString(CultureInfo.InvariantCulture);

        foreach (var recepientId in recepientIds)
        {
            userContext.PendingNotifications.Add(new PendingNotificationModel
            {
                RecepientId = recepientId,
                Type = type,
                Payload = payload,
                CreatedAt = DateTime.UtcNow,
            });
        }

        userContext.SaveChanges();
    }

    /// <summary>
    /// Событие друзей: одна запись-источник и по строке очереди на каждого получателя.
    /// </summary>
    private static void EnqueueNotification(UserContext userContext, NotificationModel notification, IReadOnlyCollection<int> recepientIds)
    {
        if (recepientIds.Count == 0)
            return;

        userContext.Notifications.Add(notification);
        userContext.SaveChanges();

        foreach (var recepientId in recepientIds)
        {
            userContext.PendingNotifications.Add(new PendingNotificationModel
            {
                RecepientId = recepientId,
                Type = ToPendingType(notification.Type),
                NotificationId = notification.NotificationId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        userContext.SaveChanges();
    }

    private static void EnqueuePending(UserContext userContext, int recepientId, PendingNotificationType type, string payload = null)
    {
        userContext.PendingNotifications.Add(new PendingNotificationModel
        {
            RecepientId = recepientId,
            Type = type,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
        });

        userContext.SaveChanges();
    }

    private static PendingNotificationType ToPendingType(NotificationMessageType type) => type switch
    {
        NotificationMessageType.NewFriend => PendingNotificationType.NewFriend,
        NotificationMessageType.NewWish => PendingNotificationType.NewWish,
        NotificationMessageType.WishEdit => PendingNotificationType.WishEdit,
        NotificationMessageType.WishDelete => PendingNotificationType.WishDelete,
        NotificationMessageType.WishPermanent => PendingNotificationType.WishPermanent,
        _ => throw new NotSupportedException($"Unexpected notification type '{type}'"),
    };
}
