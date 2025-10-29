using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.Subscription;

[AllowedTypes(QueryParameterType.SetListPageTo)]
public class MySubscribersMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.ListPositions)
            .Include(u => u.Subscribers)
            .ThenInclude(s => s.Subscriber)
            .First(u => u.UserId == userId);

        var totalCount = user.Subscribers.Count;

        if (totalCount == 0)
        {
            const string downArrow = "\u2b07\ufe0f";
            const string link = "\U0001f517";

            Text.Bold("У вас пока нет подписчиков :(")
                .LineBreak()
                .Italic($"Пригласите друзей, прислав им ссылку на свой вишлист {downArrow}");

            Keyboard.AddCopyTextButton($"{link} Ссылка на вишлист", user.GetSubscribeLink());

            Keyboard.NewRow();
        }
        else
        {
            Text.Bold("Ваши подписчики:");
        }

        ListMessageUtils.AddListControls<MySubscribersQuery, MainMenuQuery>(Keyboard, parameters, totalCount, user.ListPositions.SubscriberPage, itemIndex =>
            {
                var subscriber = user.Subscribers[itemIndex].Subscriber;

                Keyboard.AddButton<SubscriberQuery>(
                        subscriber.FirstName,
                        new QueryParameter(QueryParameterType.UserId, subscriber.UserId));
            },
            pageIndex => user.ListPositions.SubscriberPage = pageIndex);

        return Task.CompletedTask;
    }
}
