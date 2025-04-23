using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(SubscriberMessage))]
public class ConfirmUnsubscribeMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<UnsubscribeQuery>()
           .NewRow()
           .AddButton<SubscriptionQuery>("Отмена \u274c");

        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users.AsNoTracking().First(u => u.UserId == targetId);

        Text.Italic("Действительно отписаться от ")
           .InlineMention(target)
           .Italic("?");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<UnsubscribeQuery>()
           .NewRow()
           .AddButton<CompactListQuery>("Отмена \u274c");

        user = Legacy_GetUser(user, parameters);

        Text.Italic("Действительно отписаться от ")
           .InlineMention(user)
           .Italic("?");

        return Task.CompletedTask;
    }
}
