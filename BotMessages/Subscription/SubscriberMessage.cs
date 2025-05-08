using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Model;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(MySubscribersMessage))]
public class SubscriberMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users.Include(u => u.Subscriptions).First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == targetId);

        var isSenderSubscribed = sender.Subscriptions.Any(s => s.TargetId == target.UserId);

        if (isSenderSubscribed)
        {
            Keyboard.AddButton<ConfirmUnsubscribeQuery>("Отписаться");
        }
        else
        {
            Keyboard.AddButton<FinishSubscriptionQuery>("Подписаться");
        }

        Keyboard.AddButton<ConfirmDeleteSubscriberQuery>();

        if (target.Wishes.Count != 0)
        {
            Keyboard
               .NewRow()
               .AddButton<CompactListQuery>("Открыть вишлист", QueryParameter.ReturnToSubscriber);
        }

        Keyboard
           .NewRow()
           .AddButton<MySubscribersQuery>("К моим подписчикам");

        Text.Bold("Подписчик ")
           .InlineMention(target)
           .Bold(":")
           .LineBreak()
           .Bold("Вишей в вишлисте: ")
           .Monospace(target.Wishes.Count.ToString());

        return Task.CompletedTask;
    }
}
