using Serilog;
using WishlistBot.Queries.Friends;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Friends;

/// <summary>
/// Добавление в друзья: и по кнопке «Добавить в друзья», и по диплинку
/// <c>?start=&lt;SubscribeId&gt;</c>. Дружба создаётся сразу в обе стороны.
/// </summary>
[AllowedTypes(QueryParameterType.UserId)]
[MessageFor(typeof(AddFriendQuery))]
public class AddFriendMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override async Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users.Include(u => u.Friends).First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.UserId, out var targetId);
        var target = userContext.Users.First(u => u.UserId == targetId);

        if (sender.IsFriendOf(target.UserId))
        {
            Text.Italic("Вы уже друзья с ")
                .InlineMention(target)
                .Italic(".");
        }
        else
        {
            await PerformAddFriend(Logger, userContext, sender, target);

            Text.Italic("Теперь вы друзья с ")
                .InlineMention(target)
                .Italic("!");
        }

        // NOTE: no SetListPageTo here. This button opens the profile window, not a list,
        // and the page parameter used to leak from the friends count into the wishlist page.
        Keyboard
            .AddButton<FriendQuery>($"Открыть профиль {target.FirstName}",
                    new QueryParameter(QueryParameterType.UserId, target.UserId))
            .NewRow()
            .AddButton<MyFriendsQuery>("К друзьям");
    }

    public static Task PerformAddFriend(ILogger logger, UserContext userContext, UserModel user, UserModel friend)
    {
        FriendshipManager.AddFriend(userContext, user, friend);

        return NotificationService.Instance.SendNewFriend(userContext, user, friend.UserId);
    }
}
