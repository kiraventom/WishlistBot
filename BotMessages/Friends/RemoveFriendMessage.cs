using Serilog;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Friends;

public class RemoveFriendMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<MyFriendsQuery>("К друзьям");

        parameters.Peek(QueryParameterType.UserId, out var friendId);

        var sender = userContext.Users
            .Include(u => u.Friends)
            .First(u => u.UserId == userId);

        var friend = userContext.Users.First(u => u.UserId == friendId);

        Text.Italic("Вы удалили ")
           .InlineMention(friend)
           .Italic(" из друзей.");

        FriendshipManager.RemoveFriend(userContext, sender, friend);

        return Task.CompletedTask;
    }
}
