using Serilog;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

// TODO Combine common code with EditWish
[AllowedTypes(QueryParameterType.SetWishTo, QueryParameterType.ClaimWish)]
[ChildMessage(typeof(FullListMessage))]
public class ShowWishMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users.Include(u => u.Wishes)
           .ThenInclude(w => w.Links)
           .First(u => u.UserId == targetId);

        parameters.Pop(QueryParameterType.SetWishTo, out var wishId);

        var wish = target.Wishes.FirstOrDefault(w => w.WishId == wishId);
        if (wish is null)
        {
            throw new NotSupportedException($"Can't find wish {wishId} to show");
        }

        var name = wish.Name;
        var description = wish.Description;
        var links = wish.Links;
        var priceRange = wish.PriceRange;

        if (parameters.Pop(QueryParameterType.ClaimWish))
        {
            // Claim unclaimed wish
            if (wish.ClaimerId is null)
            {
                wish.ClaimerId = userId;
            }
            // Unclaim wish claimed by sender
            else if (wish.ClaimerId == userId)
            {
                wish.ClaimerId = null;
            }
            else
            {
                Logger.Error("ShowWish: parameters contain ClaimWish, but wish.ClaimerId is nor 0 neither [{senderId}], but [{claimerId}]", userId, wish.ClaimerId);
            }
        }

        if (wish.ClaimerId is not null)
        {
            var claimer = userContext.Users.FirstOrDefault(u => u.UserId == wish.ClaimerId);
            if (claimer is not null)
            {
                if (claimer.UserId == userId)
                {
                    Text.ItalicBold("Этот виш забронирован вами").LineBreak().LineBreak();
                    Keyboard.AddButton<ShowWishQuery>("Снять бронь", new QueryParameter(QueryParameterType.SetWishTo, wishId), QueryParameter.ClaimWish)
                       .NewRow();
                }
                else
                {
                    Text.ItalicBold("\u203c\ufe0f Этот виш забронирован ").InlineMention(claimer).ItalicBold("! \u203c\ufe0f").LineBreak().LineBreak();
                }
            }
            else
            {
                Logger.Error("Wish [{wishId}]: Claimer [{claimerId}] not found in database. Cleaning ClaimerId", wish.WishId, wish.ClaimerId);
                wish.ClaimerId = null;
            }
        }

        // Checking ClaimerId in separate if because it can be reset when claimer was not found in database
        if (wish.ClaimerId is null)
        {
            Keyboard.AddButton<ShowWishQuery>("Забронировать", new QueryParameter(QueryParameterType.SetWishTo, wishId), QueryParameter.ClaimWish)
               .NewRow();
        }

        Text.Bold("Название: ").Monospace(name);

        if (description is not null)
            Text.LineBreak().Bold("Описание: ").LineBreak().ExpandableQuote(description);

        if (links.Any())
        {
            Text.LineBreak().Bold("Ссылки: ");
            for (var i = 0; i < links.Count; ++i)
            {
                var link = links[i].Url;
                Text.InlineUrl(link);
                if (i < links.Count - 1)
                    Text.Verbatim(", ");
            }
        }

        if (priceRange != Price.NotSet)
        {
            var priceRangeString = MessageTextUtils.PriceToString(priceRange);
            Text.LineBreak().Bold("Цена: ").Monospace(priceRangeString);
        }

        PhotoFileId = wish.FileId;

        Keyboard.AddButton<FullListQuery>("Назад", QueryParameter.ReadOnly);

        return Task.CompletedTask;
    }
}
