using Serilog;
using Telegram.Bot;
using WishlistBot.BotMessages;
using WishlistBot.Jobs;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;
using WishlistBot.BotMessages.Notification;

namespace WishlistBot.Notification;

public class NotificationService
{
    private bool _inited;

    private ILogger _logger;
    private ITelegramBotClient _client;

    public static NotificationService Instance { get; } = new();

    public void Init(ILogger logger, ITelegramBotClient client)
    {
        if (_inited)
            return;

        _logger = logger;
        _client = client;

        _inited = true;
    }

    public Task SendToSubscribers(NotificationModel notification, UserContext userContext)
    {
        var notificationSource = userContext.Users
            .Include(c => c.Subscribers)
            .Include(c => c.Settings)
            .First(u => u.UserId == notification.SourceId);

        if (!notificationSource.Settings.SendNotifications)
            return Task.CompletedTask;

        var recepients = notificationSource.Subscribers.Select(c => c.SubscriberId).ToList();

        userContext.Notifications.Add(notification);
        userContext.SaveChanges();

        JobManager.Instance.StartJob($"Notification from {notificationSource.FirstName}", notification.NotificationId, recepients, TimeSpan.FromSeconds(1), /* TODO Fix */ (NotificationJobActionDelegate)SendToSubscriber);
        return Task.CompletedTask;
    }

    public async Task SendToUser(BotMessage notification, UserContext userContext, int notificationRecepientId)
    {
        var notificationRecepient = userContext.Users.Include(u => u.Settings).First(u => u.UserId == notificationRecepientId);
        if (notificationRecepient.Settings.ReceiveNotifications)
            await _client.SendOrEditBotMessage(_logger, userContext, notificationRecepientId, notification, forceNewMessage: true);
    }

    public async Task<int> BroadcastToUser(BotMessage notification, UserContext userContext, int notificationRecepientId)
    {
        var notificationRecepient = userContext.Users.First(u => u.UserId == notificationRecepientId);
        var message = await _client.SendOrEditBotMessage(_logger, userContext, notificationRecepientId, notification, forceNewMessage: true);
        return message.MessageId;
    }

    private async Task SendToSubscriber(ILogger logger, ITelegramBotClient client, UserContext userContext, int userId, int notificationId)
    {
        var recepient = userContext.Users.Include(c => c.Settings).FirstOrDefault(u => u.UserId == userId);
        var notification = userContext.Notifications.First(n => n.NotificationId == notificationId);

        // TODO: NotificationFactory.cs?
        BotMessage botMessage = notification.Type switch
        {
            NotificationMessageType.NewSubscriber => new NewSubscriberNotificationMessage(logger, notification),
            NotificationMessageType.NewWish => new NewWishNotificationMessage(logger, notification),
            NotificationMessageType.WishEdit => new EditWishNotificationMessage(logger, notification),
            NotificationMessageType.WishDelete => new DeleteWishNotificationMessage(logger, notification),
            _ => throw new NotSupportedException($"Unexpected notification message type '{notification.Type}'")
        };

        if (recepient.Settings.ReceiveNotifications)
            await _client.SendOrEditBotMessage(_logger, userContext, userId, botMessage, forceNewMessage: true);
    }
}
