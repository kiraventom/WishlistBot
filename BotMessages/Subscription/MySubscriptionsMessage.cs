using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetListPageTo, QueryParameterType.ReadOnly)]
public class MySubscriptionsMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
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
                    QueryParameter.ReadOnly,
                    new QueryParameter(QueryParameterType.SetUserTo, userWeSubscribedTo.UserId),
                    new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
        });

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var totalCount = user.Subscriptions.Count;

        Text.Bold(totalCount == 0 ? "Вы ещё ни на кого не подписаны :(" : "Ваши подписки:");

        ListMessageUtils.AddListControls<MySubscriptionsQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            var subscribeId = user.Subscriptions[itemIndex];
            var userWeSubscribedTo = Users.FirstOrDefault(u => u.SubscribeId == subscribeId);
            if (userWeSubscribedTo is null)
            {
                Logger.Error("Users DB does not contain user wish subscribe id [{subscribeId}]", subscribeId);
                return;
            }

            Keyboard.AddButton<SubscriptionQuery>(
             userWeSubscribedTo.FirstName,
             QueryParameter.ReadOnly,
             new QueryParameter(QueryParameterType.SetUserTo, userWeSubscribedTo.SenderId),
             new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
        });

        return Task.CompletedTask;
    }
}
