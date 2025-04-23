using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

public class DeleteSubscriberMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<MySubscribersQuery>("К моим подписчикам");

        var sender = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users
            .Include(u => u.Subscriptions)
            .Include(u => u.ClaimedWishes)
            .First(u => u.UserId == targetId);

        Text.Italic("Вы удалили ")
           .InlineMention(target)
           .Italic(" из списка своих подписчиков.");

        var subscription = target.Subscriptions.First(s => s.SubscriberId == target.UserId);
        target.Subscriptions.Remove(subscription);
        target.ClaimedWishes.RemoveAll(cw => cw.Owner == sender);

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard.AddButton<MySubscribersQuery>("К моим подписчикам");

        var sender = user;

        user = Legacy_GetUser(user, parameters);

        Text.Italic("Вы удалили ")
           .InlineMention(user)
           .Italic(" из списка своих подписчиков.");

        user.Subscriptions.Remove(sender.SubscribeId);
        foreach (var claimedWish in sender.Wishes.Where(w => w.ClaimerId == user.SenderId))
        {
            claimedWish.ClaimerId = 0;
        }

        return Task.CompletedTask;
    }
}
