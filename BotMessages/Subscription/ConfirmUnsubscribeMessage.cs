using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(SubscriberMessage))]
public class ConfirmUnsubscribeMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<UnsubscribeQuery>()
           .NewRow()
           .AddButton<SubscriptionQuery>("Отмена \u274c");

        parameters.Peek(QueryParameterType.UserId, out var targetId);
        var target = userContext.Users.AsNoTracking().First(u => u.UserId == targetId);

        Text.Italic("Действительно отписаться от ")
           .InlineMention(target)
           .Italic("?");

        return Task.CompletedTask;
    }
}
