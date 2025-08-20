using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetListPageTo)]
[ChildMessage(typeof(CompactListMessage))]
public class FullListMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users.Include(u => u.Wishes).AsNoTracking();
        var (sender, targetUser) = GetSenderAndTarget(users, userId, parameters);

        var isReadOnly = sender != targetUser;

        var totalCount = targetUser.Wishes.Count;
        var sortedWishes = targetUser.GetSortedWishes();

        ListMessageUtils.AddListControls<FullListQuery, CompactListQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            if (isReadOnly)
                AddShowWishButton(sortedWishes, itemIndex, pageIndex);
            else
                AddEditWishButton(sortedWishes, itemIndex, pageIndex);
        });

        if (totalCount == 0)
        {
            Text.Bold("Список пуст");
            return Task.CompletedTask;
        }

        if (isReadOnly)
            Text.Bold("Виши ").InlineMention(targetUser).Bold(":");
        else
            Text.Bold("Ваши виши:");

        return Task.CompletedTask;
    }

    private void AddShowWishButton(IReadOnlyList<WishModel> sortedWishes, int itemIndex, int pageIndex)
    {
        var wish = sortedWishes[itemIndex];

        const string eyeEmoji = "\U0001f441\ufe0f ";

        var isClaimed = wish.ClaimerId is not null;
        var claimedText = isClaimed ? "[БРОНЬ] " : string.Empty;

        Keyboard.AddButton<ShowWishQuery>(
           claimedText + eyeEmoji + wish.Name,
           new QueryParameter(QueryParameterType.SetWishTo, wish.WishId),
           new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
           QueryParameter.ReturnToFullList);
    }

    private void AddEditWishButton(IReadOnlyList<WishModel> sortedWishes, int itemIndex, int pageIndex)
    {
        var wish = sortedWishes[itemIndex];

        const string pencilEmoji = "\u270f\ufe0f ";

        Keyboard.AddButton<EditWishQuery>(
           pencilEmoji + wish.Name,
           new QueryParameter(QueryParameterType.SetWishTo, wish.WishId),
           new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
           QueryParameter.ReturnToFullList);
    }
}
