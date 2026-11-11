using Serilog;
using Telegram.Bot;
using WishlistBot.BotMessages;
using WishlistBot.Jobs;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.BotMessages.Notification;

namespace WishlistBot.Notification;

public class NotificationService
{
    /// <summary>Название топика, куда складываются уведомления.</summary>
    private const string NotificationTopicName = "\U0001f514 Уведомления";

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

    public Task SendToFriends(NotificationModel notification, UserContext userContext)
    {
        var notificationSource = userContext.Users
            .Include(c => c.Friends)
            .Include(c => c.Settings)
            .First(u => u.UserId == notification.SourceId);

        if (!notificationSource.Settings.SendNotifications)
            return Task.CompletedTask;

        var recepients = notificationSource.Friends.Select(f => f.FriendId).ToList();

        userContext.Notifications.Add(notification);
        userContext.SaveChanges();

        JobManager.Instance.StartJob($"Notification from {notificationSource.FirstName}", notification.NotificationId, recepients, TimeSpan.FromSeconds(1), /* TODO Fix */ (NotificationJobActionDelegate)SendToRecepient);
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

        userContext.Notifications.Add(notification);
        userContext.SaveChanges();

        JobManager.Instance.StartJob($"Notification from {notificationSource.FirstName}", notification.NotificationId, [claimerId], TimeSpan.FromSeconds(1), /* TODO Fix */ (NotificationJobActionDelegate)SendToRecepient);
        return Task.CompletedTask;
    }

    public async Task SendToUser(BotMessage notification, UserContext userContext, int notificationRecepientId)
    {
        var notificationRecepient = userContext.Users.Include(u => u.Settings).First(u => u.UserId == notificationRecepientId);
        if (!notificationRecepient.Settings.ReceiveFriendWishNotifications)
            return;

        await SendToNotificationTopic(notification, userContext, notificationRecepient);
    }

    /// <summary>
    /// Отправка уведомления в топик «Уведомления». Топик создаётся лениво; если
    /// пользователь его удалил (или закрыл) — пересоздаём и пробуем ещё раз. Если топики
    /// недоступны вовсе (например, у бота выключена настройка в BotFather) — уведомление
    /// уходит в основной чат, чтобы не потеряться.
    /// </summary>
    private async Task SendToNotificationTopic(BotMessage notification, UserContext userContext, UserModel recepient)
    {
        int? threadId = null;

        try
        {
            threadId = await EnsureNotificationTopic(userContext, recepient);

            await _client.SendOrEditBotMessage(_logger, userContext, recepient.UserId, notification,
                forceNewMessage: true, messageThreadId: threadId, throwOnError: true);

            NotifyDelivered(userContext, recepient);

            return;
        }
        catch (Exception e) when (threadId is not null && IsTopicUnavailable(e))
        {
            _logger?.Warning("Notification topic [{threadId}] of user [{userId}] is gone: {error}",
                threadId, recepient.UserId, e.Message);

            recepient.NotificationTopicId = null;
            userContext.SaveChanges();
        }
        catch (Exception e)
        {
            _logger?.Error("Failed to send notification into the topic of user [{userId}]: {error}",
                recepient.UserId, e.Message);
        }

        // топик удалён или недоступен — пробуем ещё раз с новым топиком,
        // а если и это не выходит, отправляем в основной чат
        try
        {
            var newThreadId = await EnsureNotificationTopic(userContext, recepient);

            await _client.SendOrEditBotMessage(_logger, userContext, recepient.UserId, notification,
                forceNewMessage: true, messageThreadId: newThreadId, throwOnError: true);

            NotifyDelivered(userContext, recepient);
        }
        catch (Exception e)
        {
            _logger?.Warning("Can't deliver notification of user [{userId}] into a topic ({error}), sending to the main chat",
                recepient.UserId, e.Message);

            // последний шанс: основной чат. Ошибку не пробрасываем — уведомление не должно
            // ломать апдейт или фоновую задачу
            try
            {
                await _client.SendOrEditBotMessage(_logger, userContext, recepient.UserId, notification, forceNewMessage: true);

                NotifyDelivered(userContext, recepient);
            }
            catch (Exception fallbackError)
            {
                _logger?.Error("Failed to deliver notification of user [{userId}] at all: {error}",
                    recepient.UserId, fallbackError.Message);
            }
        }
    }

    /// <summary>
    /// Помечает, что с последнего экрана пользователю приходило уведомление: следующий
    /// экран уйдёт новым сообщением и в ленте «All» окажется под уведомлением.
    /// </summary>
    private static void NotifyDelivered(UserContext userContext, UserModel recepient)
    {
        recepient.NotifiedSinceLastMessage = true;
        userContext.SaveChanges();
    }

    /// <summary>Возвращает id топика уведомлений, создавая его при необходимости.</summary>
    private async Task<int> EnsureNotificationTopic(UserContext userContext, UserModel user)
    {
        if (user.NotificationTopicId is { } existing)
            return existing;

        var topic = await _client.CreateForumTopic(chatId: user.TelegramId, name: NotificationTopicName);

        user.NotificationTopicId = topic.MessageThreadId;
        userContext.SaveChanges();

        _logger?.Information("Created notification topic [{threadId}] for user [{userId}]",
            topic.MessageThreadId, user.UserId);

        return topic.MessageThreadId;
    }

    /// <summary>
    /// Ошибка означает, что топика больше нет (пользователь удалил его) или он закрыт.
    /// Telegram отвечает разными формулировками ("message thread not found",
    /// "TOPIC_ID_INVALID"), поэтому смотрим на текст.
    /// </summary>
    private static bool IsTopicUnavailable(Exception exception) =>
        exception.Message.Contains("thread", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("topic", StringComparison.OrdinalIgnoreCase);

    public async Task<int> BroadcastToUser(BotMessage notification, UserContext userContext, int notificationRecepientId)
    {
        var notificationRecepient = userContext.Users.First(u => u.UserId == notificationRecepientId);
        var message = await _client.SendOrEditBotMessage(_logger, userContext, notificationRecepientId, notification, forceNewMessage: true);
        return message.MessageId;
    }

    private async Task SendToRecepient(ILogger logger, ITelegramBotClient client, UserContext userContext, int userId, int notificationId)
    {
        var recepient = userContext.Users.Include(c => c.Settings).FirstOrDefault(u => u.UserId == userId);
        var notification = userContext.Notifications.First(n => n.NotificationId == notificationId);

        // TODO: NotificationFactory.cs?
        BotMessage botMessage = notification.Type switch
        {
            NotificationMessageType.NewFriend => new NewFriendNotificationMessage(logger, notification),
            NotificationMessageType.NewWish => new NewWishNotificationMessage(logger, notification),
            NotificationMessageType.WishEdit => new EditWishNotificationMessage(logger, notification),
            NotificationMessageType.WishDelete => new DeleteWishNotificationMessage(logger, notification),
            NotificationMessageType.WishPermanent => new WishPermanentNotificationMessage(logger, notification),
            _ => throw new NotSupportedException($"Unexpected notification message type '{notification.Type}'")
        };

        if (recepient.Settings.ReceiveFriendWishNotifications)
            await SendToNotificationTopic(botMessage, userContext, recepient);
    }
}
