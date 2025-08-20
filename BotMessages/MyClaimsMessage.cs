using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Queries;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.SetListPageTo)]
public class MyClaimsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Bold("Здесь отображаются забронированные вами виши.");

        var users = userContext.Users
            .Include(u => u.ClaimedWishes)
            .ThenInclude(cw => cw.Owner)
            .AsNoTracking();

        var (sender, _) = GetSenderAndTarget(users, userId, parameters);

        var totalCount = sender.ClaimedWishes.Count;

        ListMessageUtils.AddListControls<MyClaimsQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
        var claimedWish = sender.ClaimedWishes[itemIndex];

        Keyboard.AddButton<ShowWishQuery>(
                $"{claimedWish.Owner.FirstName}: {claimedWish.Name}",
                new QueryParameter(QueryParameterType.SetUserTo, claimedWish.OwnerId),
                new QueryParameter(QueryParameterType.SetWishTo, claimedWish.WishId),
                new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
                QueryParameter.ReturnToMyClaims);
        });

        return Task.CompletedTask;
    }
}

