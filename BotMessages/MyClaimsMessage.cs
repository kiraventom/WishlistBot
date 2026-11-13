using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.Text;
using WishlistBot.BotMessages.Friends;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedListPositions(ListPosition.Claim)]
[AllowedTypes(QueryParameterType.SetListPageTo, QueryParameterType.ClaimWish, QueryParameterType.WishId)]
[MessageFor(typeof(MyClaimsQuery))]
public class MyClaimsMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var users = userContext.Users
            .Include(u => u.ListPositions).ThenInclude(u => u.ClaimPage)
            .Include(u => u.ClaimedWishes).ThenInclude(cw => cw.Owner).ThenInclude(owner => owner.Profile);

        var (sender, _) = GetSenderAndTarget(users, userId, parameters);

        if (parameters.Pop(QueryParameterType.ClaimWish))
        {
            parameters.Pop(QueryParameterType.WishId, out var wishId);
            var claimedWish = sender.ClaimedWishes.First(cw => cw.WishId == wishId);
            claimedWish.ClaimerId = null;
            userContext.SaveChanges();
        }

        var claimedWishes = sender.GetSortedClaimedWishes();

        Text.Heading("Ваши брони");

        var page = TextListMessageUtils.AddListControls<MyClaimsQuery, MainMenuQuery>(
            Keyboard, parameters, claimedWishes.Count, sender.ListPositions.ClaimPage, buttonCaption: "Страница");

        if (page.Count == 0)
            return Task.CompletedTask;

        var today = DateOnly.FromDateTime(DateTime.Now);

        Text.Table(table =>
        {
            table
                .Header("Друг", "Виш", "Ближайший ДР")
                .Align(TableAlign.Left, TableAlign.Left, TableAlign.Left);

            for (var i = 0; i < page.Count; ++i)
            {
                var claimedWish = claimedWishes[page.FirstIndex + i];

                table.Row(
                    cell => cell.InlineMention(claimedWish.Owner),
                    cell => cell.InlineUrl(claimedWish.Name, claimedWish.BuildLink(claimedWish.Owner.SubscribeId, returnToClaims: true)),
                    cell => FriendBirthdayLine.AddBirthdayCell(cell, claimedWish.Owner.Profile?.Birthday, today));
            }
        });

        return Task.CompletedTask;
    }
}
