using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.ReadOnly, QueryParameterType.SetListPageTo)]
[ChildMessage(typeof(CompactListMessage))]
public class FullListMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var isReadOnly = parameters.Peek(QueryParameterType.ReadOnly);

        parameters.Peek(QueryParameterType.SetUserTo, out var targetUserId);
        var targetUser = userContext.Users
            .Include(u => u.Wishes)
            .First(u => u.UserId == targetUserId);

        var totalCount = targetUser.Wishes.Count;

        ListMessageUtils.AddListControls<FullListQuery, CompactListQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            if (isReadOnly)
                AddShowWishButton(userContext, targetUser.UserId, itemIndex, pageIndex);
            else
                AddEditWishButton(userContext, targetUser.UserId, itemIndex, pageIndex);
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

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var isReadOnly = parameters.Peek(QueryParameterType.ReadOnly);
        user = Legacy_GetUser(user, parameters);

        var totalCount = user.Wishes.Count;

        ListMessageUtils.AddListControls<FullListQuery, CompactListQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
        {
            if (isReadOnly)
                Legacy_AddShowWishButton(user, itemIndex, pageIndex);
            else
                Legacy_AddEditWishButton(user, itemIndex, pageIndex);
        });

        if (totalCount == 0)
        {
            Text.Bold("Список пуст");
            return Task.CompletedTask;
        }

        if (isReadOnly)
            Text.Bold("Виши ").InlineMention(user).Bold(":");
        else
            Text.Bold("Ваши виши:");

        return Task.CompletedTask;
    }

    private void AddShowWishButton(UserContext userContext, int userId, int itemIndex, int pageIndex)
    {
        var user = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == userId);
        var wish = user.Wishes[itemIndex];

        const string eyeEmoji = "\U0001f441\ufe0f ";

        var isClaimed = wish.ClaimerId != 0;
        var claimedText = isClaimed ? "[БРОНЬ] " : string.Empty;

        Keyboard.AddButton<ShowWishQuery>(
           claimedText + eyeEmoji + wish.Name,
           new QueryParameter(QueryParameterType.SetWishTo, wish.WishId),
           new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
           QueryParameter.ReturnToFullList);
    }

    private void Legacy_AddShowWishButton(BotUser user, int itemIndex, int pageIndex)
    {
        var wish = user.Wishes[itemIndex];

        const string eyeEmoji = "\U0001f441\ufe0f ";

        var isClaimed = wish.ClaimerId != 0;
        var claimedText = isClaimed ? "[БРОНЬ] " : string.Empty;

        Keyboard.AddButton<ShowWishQuery>(
           claimedText + eyeEmoji + wish.Name,
           new QueryParameter(QueryParameterType.SetWishTo, wish.Id),
           new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
           QueryParameter.ReturnToFullList);
    }

    private void AddEditWishButton(UserContext userContext, int userId, int itemIndex, int pageIndex)
    {
        var user = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == userId);
        var wish = user.Wishes[itemIndex];

        const string pencilEmoji = "\u270f\ufe0f ";

        Keyboard.AddButton<EditWishQuery>(
           pencilEmoji + wish.Name,
           new QueryParameter(QueryParameterType.SetWishTo, wish.WishId),
           new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
           QueryParameter.ReturnToFullList);
    }

    private void Legacy_AddEditWishButton(BotUser user, int itemIndex, int pageIndex)
    {
        var wish = user.Wishes[itemIndex];

        const string pencilEmoji = "\u270f\ufe0f ";

        Keyboard.AddButton<EditWishQuery>(
           pencilEmoji + wish.Name,
           new QueryParameter(QueryParameterType.SetWishTo, wish.Id),
           new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
           QueryParameter.ReturnToFullList);
    }
}
