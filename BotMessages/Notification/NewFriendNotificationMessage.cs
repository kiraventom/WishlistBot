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

        var notificationTarget = userContext.Users
           .Include(u => u.Friends).ThenInclude(f => f.Friend)
           .First(u => u.UserId == userId);

        var friends = notificationTarget.Friends.Select(f => f.Friend).ToList();

        // TODO Fix
        friends.Sort((f0, f1) => f0.UserId.CompareTo(f1.UserId));
        var friendIndex = friends.IndexOf(notificationSource);
        var pageIndex = friendIndex / TextListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<FriendQuery>("Перейти к другу",
                                     new QueryParameter(QueryParameterType.UserId, notificationSource.UserId),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        Text
           .InlineMention(notificationSource)
           .Italic(" добавил вас в друзья!");

        return Task.CompletedTask;
    }
}
