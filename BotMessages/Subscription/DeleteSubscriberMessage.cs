using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

public class DeleteSubscriberMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<MySubscribersQuery>("К моим подписчикам");

        parameters.Peek(QueryParameterType.UserId, out var subscriberId);
        var subscriber = userContext.Users
            .Include(u => u.Subscriptions)
            .Include(u => u.ClaimedWishes)
            .First(u => u.UserId == subscriberId);

        Text.Italic("Вы удалили ")
           .InlineMention(subscriber)
           .Italic(" из списка своих подписчиков.");

        var subscription = subscriber.Subscriptions.First(s => s.TargetId == userId);
        subscriber.Subscriptions.Remove(subscription);
        subscriber.ClaimedWishes.RemoveAll(cw => cw.OwnerId == userId);

        return Task.CompletedTask;
    }
}
