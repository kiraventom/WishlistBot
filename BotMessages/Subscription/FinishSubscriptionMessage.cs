using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetUserTo)]
public class FinishSubscriptionMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override async Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users.Include(u => u.Subscriptions).First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
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

            var newSubscription = new SubscriptionModel()
            {
                Target = target,
            };

            sender.Subscriptions.Add(newSubscription);

            var newSubscriberNotification = new NewSubscriberNotificationMessage(Logger, sender.UserId);
            await NotificationService.Instance.SendToUser(newSubscriberNotification, userContext, target.UserId);
        }

        Keyboard
           .AddButton<CompactListQuery>($"Открыть вишлист {target.FirstName}",
                                        QueryParameter.ReadOnly,
                                        new QueryParameter(QueryParameterType.SetUserTo, target.UserId))
           .NewRow()
           .AddButton<MySubscriptionsQuery>("К моим подпискам");
    }
}
