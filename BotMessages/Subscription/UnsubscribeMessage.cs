using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

public class UnsubscribeMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<MySubscriptionsQuery>("К моим подпискам");

        var sender = userContext.Users.Include(u => u.Subscriptions).First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == targetId);

        Text.Italic("Вы отписались от вишлиста ")
           .InlineMention(target);

        var subscription = sender.Subscriptions.First(s => s.TargetId == target.UserId);
        sender.Subscriptions.Remove(subscription);
        foreach (var claimedWish in sender.Wishes.Where(w => w.ClaimerId == target.UserId))
        {
            claimedWish.ClaimerId = null;
        }

        return Task.CompletedTask;
    }
}
