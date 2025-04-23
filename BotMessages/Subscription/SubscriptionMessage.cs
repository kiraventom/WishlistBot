using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Model;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(MySubscriptionsMessage))]
public class SubscriptionMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == targetId);

        Keyboard.AddButton<ConfirmUnsubscribeQuery>("Отписаться");

        if (target.Wishes.Count != 0)
        {
            Keyboard
               .NewRow()
               .AddButton<CompactListQuery>("Открыть вишлист");
        }

        Keyboard
           .NewRow()
           .AddButton<MySubscriptionsQuery>("К моим подпискам");

        Text.Bold("Подписка на ")
           .InlineMention(target)
           .Bold(":")
           .LineBreak()
           .Bold("Вишей в вишлисте: ")
           .Monospace(target.Wishes.Count.ToString());

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        user = Legacy_GetUser(user, parameters);

        Keyboard.AddButton<ConfirmUnsubscribeQuery>("Отписаться");

        if (user.Wishes.Count != 0)
        {
            Keyboard
               .NewRow()
               .AddButton<CompactListQuery>("Открыть вишлист");
        }

        Keyboard
           .NewRow()
           .AddButton<MySubscriptionsQuery>("К моим подпискам");

        Text.Bold("Подписка на ")
           .InlineMention(user)
           .Bold(":")
           .LineBreak()
           .Bold("Вишей в вишлисте: ")
           .Monospace(user.Wishes.Count.ToString());

        return Task.CompletedTask;
    }
}
