using Serilog;
using WishlistBot.Notification;
using WishlistBot.Queries;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Notification;

public class NewFriendNotificationMessage : BotMessage, INotificationMessage
{
    private readonly int _notificationSourceId;

    public NewFriendNotificationMessage(ILogger logger, NotificationModel notification) : base(logger)
    {
        _notificationSourceId = notification.SourceId;
    }

    public NewFriendNotificationMessage(ILogger logger, int notificationSourceId) : base(logger)
    {
        _notificationSourceId = notificationSourceId;
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var notificationSource = userContext.Users.First(u => u.UserId == _notificationSourceId);

        Text.LineBreak().LineBreak()
            .InlineUrl("Перейти к другу", DeepLink.BuildFriend(notificationSource.SubscribeId[..20]))
            .LineBreak()
            .InlineUrl("В главное меню", DeepLink.BuildMainMenu());

        Text
           .InlineMention(notificationSource)
           .Italic(" добавил вас в друзья!");

        Text.AddNotificationsSettingsLink();

        return Task.CompletedTask;
    }
}
