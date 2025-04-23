using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetListPageTo, QueryParameterType.ReadOnly)]
public class MySubscribersMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.Subscribers)
            .ThenInclude(s => s.Subscriber)
            .First(u => u.UserId == userId);

        var totalCount = user.Subscribers.Count;

        Text.Bold(totalCount == 0 ? "У вас пока нет подписчиков :(" : "Ваши подписчики:");

        ListMessageUtils.AddListControls<MySubscribersQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            var subscriber = user.Subscribers[itemIndex].Subscriber;

            Keyboard.AddButton<SubscriberQuery>(
                    subscriber.FirstName,
                    QueryParameter.ReadOnly,
                    new QueryParameter(QueryParameterType.SetUserTo, subscriber.UserId),
                    new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
        });

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var subscribers = Users
           .Where(u => u.Subscriptions.Contains(user.SubscribeId))
           .ToList();

        var totalCount = subscribers.Count;

        Text.Bold(totalCount == 0 ? "У вас пока нет подписчиков :(" : "Ваши подписчики:");

        ListMessageUtils.AddListControls<MySubscribersQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            var subscriber = subscribers[itemIndex];

            Keyboard.AddButton<SubscriberQuery>(
             subscriber.FirstName,
             QueryParameter.ReadOnly,
             new QueryParameter(QueryParameterType.SetUserTo, subscriber.SenderId),
             new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
        });

        return Task.CompletedTask;
    }
}
