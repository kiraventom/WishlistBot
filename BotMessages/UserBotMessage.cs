using Serilog;
using WishlistBot.Model;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.SetUserTo)]
public abstract class UserBotMessage(ILogger logger) : BotMessage(logger)
{
    protected (UserModel sender, UserModel target) GetSenderAndTarget(IQueryable<UserModel> users, int userId, QueryParameterCollection parameters)
    {
        var sender = users.First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.SetUserTo, out var targetUserId);
        var targetUser = users.FirstOrDefault(u => u.UserId == targetUserId);

        if (targetUser is null)
            targetUser = sender;

        return (sender, targetUser);
    }
}

