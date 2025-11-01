using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedListPositions(ListPosition.Subscription)]
[AllowedTypes(QueryParameterType.SetListPageTo)]
public class MySubscriptionsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(l => l.SubscriptionPage)
            .Include(u => u.Subscriptions).ThenInclude(s => s.Target)
            .First(u => u.UserId == userId);

        var totalCount = user.Subscriptions.Count;

        Text.Bold(totalCount == 0 ? "Вы ещё ни на кого не подписаны :(" : "Ваши подписки:");

        ListMessageUtils.AddListControls<MySubscriptionsQuery, MainMenuQuery>(Keyboard, parameters, totalCount, user.ListPositions.SubscriptionPage, itemIndex =>
        {
            var userWeSubscribedTo = user.Subscriptions[itemIndex].Target;

            Keyboard.AddButton<SubscriptionQuery>(
                    userWeSubscribedTo.FirstName,
                    new QueryParameter(QueryParameterType.UserId, userWeSubscribedTo.UserId));
        });

        return Task.CompletedTask;
    }
}
