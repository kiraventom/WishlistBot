using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedListPositions(ListPosition.Claim)]
[AllowedTypes(QueryParameterType.SetListPageTo, QueryParameterType.ClaimWish, QueryParameterType.WishId)]
[MessageFor(typeof(MyClaimsQuery))]
public class MyClaimsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Bold("Здесь отображаются забронированные вами виши.");

        var users = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(u => u.ClaimPage)
            .Include(u => u.ClaimedWishes).ThenInclude(cw => cw.Owner);

        var (sender, _) = GetSenderAndTarget(users, userId, parameters);

        if (parameters.Pop(QueryParameterType.ClaimWish))
        {
            parameters.Pop(QueryParameterType.WishId, out var wishId);
            var claimedWish = sender.ClaimedWishes.First(cw => cw.WishId == wishId);
            claimedWish.ClaimerId = null;
            userContext.SaveChanges();
        }

        var claimedWishes = sender.GetSortedClaimedWishes();

        var totalCount = claimedWishes.Count;

        if (totalCount != 0)
            Text.LineBreak().LineBreak();

        TextListMessageUtils.AddListControls<MyClaimsQuery, MainMenuQuery>(Text, Keyboard, parameters, totalCount, sender.ListPositions.ClaimPage, itemIndex =>
        {
            var claimedWish = claimedWishes[itemIndex];

            Text.Bold($"{itemIndex + 1}. ");
            Text.InlineMention(claimedWish.Owner);
            Text.Verbatim(": ");
            Text.InlineUrl(claimedWish.Name, claimedWish.BuildLink(claimedWish.Owner.SubscribeId, returnToClaims: true));
        }, buttonCaption: "Страница");

        return Task.CompletedTask;
    }
}

