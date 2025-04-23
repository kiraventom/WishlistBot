using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.Notification;
using WishlistBot.BotMessages.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetUserTo)]
public class FinishSubscriptionMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
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

    protected override async Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var sender = user;
        user = Legacy_GetUser(user, parameters);

        if (sender.Subscriptions.Contains(user.SubscribeId))
        {
            Text.Italic("Вы уже подписаны на вишлист ")
               .InlineMention(user)
               .Italic(".");
        }
        else
        {
            Text.Italic("Вы успешно подписались на вишлист ")
               .InlineMention(user)
               .Italic("!");

            sender.Subscriptions.Add(user.SubscribeId);

            var newSubscriberNotification = new NewSubscriberNotificationMessage(Logger, sender, Users);
            await NotificationService.Instance.Legacy_SendToUser(newSubscriberNotification, user);
        }

        Keyboard
           .AddButton<CompactListQuery>($"Открыть вишлист {user.FirstName}",
                                        QueryParameter.ReadOnly,
                                        new QueryParameter(QueryParameterType.SetUserTo, user.SenderId))
           .NewRow()
           .AddButton<MySubscriptionsQuery>("К моим подпискам");
    }
}
