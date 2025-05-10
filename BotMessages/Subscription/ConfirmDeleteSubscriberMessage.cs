using Serilog;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[ChildMessage(typeof(SubscriberMessage))]
public class ConfirmDeleteSubscriberMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<DeleteSubscriberQuery>()
           .NewRow()
           .AddButton<SubscriberQuery>("Отмена \u274c");

        parameters.Peek(QueryParameterType.SetUserTo, out var subscriberId);
        var subscriber = userContext.Users.AsNoTracking().First(u => u.UserId == subscriberId);

        Text.Italic("Действительно удалить ")
           .InlineMention(subscriber)
           .Italic(" из списка подписчиков?")
           .LineBreak()
           .ItalicBold("После этого ")
           .InlineMention(subscriber)
           .ItalicBold(" больше не сможет видеть ваши виши.");

        return Task.CompletedTask;
    }
}
