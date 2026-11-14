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

        // Порядок друзей должен совпадать с экраном «Мои друзья» (ближайший ДР, число вишей,
        // имя) — иначе страница для кнопки считается по чужому порядку и ведёт не туда.
        var notificationTarget = userContext.Users
           .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Profile)
           .Include(u => u.Friends).ThenInclude(f => f.Friend).ThenInclude(friend => friend.Wishes)
           .First(u => u.UserId == userId);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var friends = notificationTarget.GetSortedFriends(today);

        var friendIndex = friends.FindIndex(friend => friend.Friend.UserId == _notificationSourceId);
        // Список друзей — таблица, поэтому страницы по TableRowsPerPage, а не по ItemsPerPage
        var pageIndex = TextListMessageUtils.GetPageIndex(friendIndex, TextListMessageUtils.TableRowsPerPage);

        Keyboard
           .AddButton<FriendQuery>("Перейти к другу",
                                     new QueryParameter(QueryParameterType.UserId, notificationSource.UserId),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex))
           .NewRow()
           .AddButton<MainMenuQuery>("В главное меню");

        Text
           .InlineMention(notificationSource)
           .Italic(" добавил вас в друзья!");

        Text.AddNotificationsSettingsLink();

        return Task.CompletedTask;
    }
}
