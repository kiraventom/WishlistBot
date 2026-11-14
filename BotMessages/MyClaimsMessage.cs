using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.Queries;
using WishlistBot.Text;
using WishlistBot.Media;
using WishlistBot.BotMessages.Friends;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

[AllowedListPositions(ListPosition.Claim)]
[AllowedTypes(QueryParameterType.SetListPageTo, QueryParameterType.ClaimWish, QueryParameterType.WishId)]
[MessageFor(typeof(MyClaimsQuery))]
public class MyClaimsMessage(ILogger logger) : UserBotMessage(logger)
{
    /// <summary>Колонки таблицы: Друг | Виш | ДР.</summary>
    private const int ColumnsCount = 3;

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
            Keyboard, parameters, claimedWishes.Count, sender.ListPositions.ClaimPage, buttonCaption: "Страница",
            itemsPerPage: TextListMessageUtils.TableRowsPerPage);

        if (page.Count == 0)
            return Task.CompletedTask;

        var today = DateOnly.FromDateTime(DateTime.Now);

        Text.Table(table =>
        {
            table
                .Header("Друг", "Виш", FriendBirthdayLine.BirthdayColumn)
                .Align(TableAlign.Left, TableAlign.Left, TableAlign.Left);

            // Над страницей — сколько броней осталось до неё, под ней — сколько после
            if (page.PageIndex > 0)
                TextListMessageUtils.AddRemainingRow(table, RemainingCaption(page.FirstIndex), ColumnsCount);

            // Аватарки владельцев броней — одним запросом на страницу
            var avatarIds = AvatarPackService.GetEmojiIds(
                claimedWishes.Skip(page.FirstIndex).Take(page.Count).Select(wish => wish.OwnerId));

            for (var i = 0; i < page.Count; ++i)
            {
                var claimedWish = claimedWishes[page.FirstIndex + i];
                var avatarEmojiId = avatarIds.GetValueOrDefault(claimedWish.OwnerId);

                table.Row(
                    cell =>
                    {
                        cell.AvatarEmoji(avatarEmojiId, AvatarPackService.FallbackEmoji);

                        if (!string.IsNullOrEmpty(avatarEmojiId))
                            cell.Verbatim(" ");

                        cell.InlineMention(claimedWish.Owner);
                    },
                    cell => cell.InlineUrl(claimedWish.Name, claimedWish.BuildLink(claimedWish.Owner.SubscribeId, returnToClaims: true)),
                    cell => FriendBirthdayLine.AddBirthdayCell(cell, claimedWish.Owner.Profile?.Birthday, today));
            }

            var remaining = claimedWishes.Count - (page.FirstIndex + page.Count);

            if (remaining > 0)
                TextListMessageUtils.AddRemainingRow(table, RemainingCaption(remaining), ColumnsCount);
        });

        return Task.CompletedTask;
    }

    private static string RemainingCaption(int count) =>
        TextListMessageUtils.RemainingCaption(count, PluralUtils.Claims(count));
}
