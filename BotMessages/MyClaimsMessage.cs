using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Queries;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.SetListPageTo, QueryParameterType.ClaimWish, QueryParameterType.SetWishTo)]
public class MyClaimsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Bold("Здесь отображаются забронированные вами виши.");

        var users = userContext.Users
            .Include(u => u.ClaimedWishes)
            .ThenInclude(cw => cw.Owner);

        var (sender, _) = GetSenderAndTarget(users, userId, parameters);

        if (parameters.Pop(QueryParameterType.ClaimWish))
        {
            parameters.Pop(QueryParameterType.SetWishTo, out var wishId);
            var claimedWish = sender.ClaimedWishes.First(cw => cw.WishId == wishId);
            claimedWish.ClaimerId = null;
            userContext.SaveChanges();
        }

        var claimedWishes = sender.GetSortedClaimedWishes();

        var totalCount = claimedWishes.Count;

        ListMessageUtils.AddListControls<MyClaimsQuery, MainMenuQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
        var claimedWish = claimedWishes[itemIndex];

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

