using Serilog;
using WishlistBot.Queries.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Friends;

[ChildMessage(typeof(FriendMessage))]
public class ConfirmRemoveFriendMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<RemoveFriendQuery>()
           .NewRow()
           .AddButton<FriendQuery>("Отмена \u274c");

        parameters.Peek(QueryParameterType.UserId, out var friendId);
        var friend = userContext.Users.AsNoTracking().First(u => u.UserId == friendId);

        Text.Italic("Действительно удалить ")
           .InlineMention(friend)
           .Italic(" из друзей?")
           .LineBreak()
           .ItalicBold("После этого вы больше не будете видеть вишлисты друг друга, а поставленные брони снимутся.");

        return Task.CompletedTask;
    }
}
