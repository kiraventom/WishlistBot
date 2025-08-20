using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Notification;

public class NewSubscriberNotificationMessage : BotMessage, INotificationMessage
{
    private readonly int _notificationSourceId;

    public NewSubscriberNotificationMessage(ILogger logger, NotificationModel notification) : base(logger)
    {
        _notificationSourceId = notification.SourceId;
    }

    public NewSubscriberNotificationMessage(ILogger logger, int notificationSourceId) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.First(u => u.UserId == _notificationSourceId);
        var notificationTarget = userContext.Users
           .Include(u => u.Subscribers)
           .ThenInclude(s => s.Subscriber)
           .First(u => u.UserId == userId);

        var subscribers = notificationTarget.Subscribers.Select(s => s.Subscriber).ToList();

        // TODO Fix
        subscribers.Sort((s0, s1) => s0.UserId.CompareTo(s1.UserId));
        var subscriberIndex = subscribers.IndexOf(notificationSource);
        var pageIndex = subscriberIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<SubscriberQuery>("Перейти к подписчику",
                                     new QueryParameter(QueryParameterType.SetUserTo, notificationSource.UserId),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        Text
           .InlineMention(notificationSource)
           .Italic(" подписался на ваш вишлист!");

        return Task.CompletedTask;
    }
}
