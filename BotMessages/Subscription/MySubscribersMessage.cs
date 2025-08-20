using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetListPageTo)]
public class MySubscribersMessage(ILogger logger) : UserBotMessage(logger)
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
                    new QueryParameter(QueryParameterType.SetUserTo, subscriber.UserId),
                    new QueryParameter(QueryParameterType.SetListPageTo, pageIndex));
        });

        return Task.CompletedTask;
    }
}
