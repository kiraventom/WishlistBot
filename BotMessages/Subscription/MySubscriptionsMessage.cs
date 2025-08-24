using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetListPageTo)]
public class MySubscriptionsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.Subscriptions)
            .ThenInclude(s => s.Target)
            .First(u => u.UserId == userId);

        var totalCount = user.Subscriptions.Count;

        Text.Bold(totalCount == 0 ? "Вы ещё ни на кого не подписаны :(" : "Ваши подписки:");

        ListMessageUtils.AddListControls<MySubscriptionsQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            var userWeSubscribedTo = user.Subscriptions[itemIndex].Target;

            Keyboard.AddButton<SubscriptionQuery>(
                    userWeSubscribedTo.FirstName,
                    new QueryParameter(QueryParameterType.UserId, userWeSubscribedTo.UserId),
                    new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
        });

        return Task.CompletedTask;
    }
}
