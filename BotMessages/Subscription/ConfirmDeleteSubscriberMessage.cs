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

        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users.AsNoTracking().First(u => u.UserId == targetId);

        Text.Italic("Действительно удалить ")
           .InlineMention(target)
           .Italic(" из списка подписчиков?")
           .LineBreak()
           .ItalicBold("После этого ")
           .InlineMention(target)
           .ItalicBold(" больше не сможет видеть ваши виши.");

        return Task.CompletedTask;
    }
}
