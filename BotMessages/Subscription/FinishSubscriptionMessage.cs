using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.UserId)]
public class FinishSubscriptionMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override async Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users.Include(u => u.Subscriptions).First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.UserId, out var targetId);
        var target = userContext.Users.First(u => u.UserId == targetId);

        if (sender.Subscriptions.Any(s => s.TargetId == target.UserId))
        {
            Text.Italic("Вы уже подписаны на вишлист ")
                .InlineMention(target)
                .Italic(".");
        }
        else
        {
            Text.Italic("Вы успешно подписались на вишлист ")
                .InlineMention(target)
                .Italic("!");
        }

        await PerformSubscription(Logger, userContext, sender, target);

        var totalSubscriptions = sender.Subscriptions.Count;
        var lastPage = totalSubscriptions / ListMessageUtils.ItemsPerPage;

        Keyboard
            .AddButton<SubscriptionQuery>($"Открыть вишлист {target.FirstName}",
                    new QueryParameter(QueryParameterType.UserId, target.UserId),
                    new QueryParameter(QueryParameterType.SetListPageTo, lastPage))
            .NewRow()
            .AddButton<MySubscriptionsQuery>("К моим подпискам");
    }

    public static Task PerformSubscription(ILogger logger, UserContext userContext, UserModel subscriber, UserModel target)
    {
        var newSubscription = new SubscriptionModel()
        {
            Target = target,
        };

        userContext.Entry(subscriber).Collection(s => s.Subscriptions).Load();
        subscriber.Subscriptions.Add(newSubscription);

        var newSubscriberNotification = new NewSubscriberNotificationMessage(logger, subscriber.UserId);
        return NotificationService.Instance.SendToUser(newSubscriberNotification, userContext, target.UserId);
    }
}
