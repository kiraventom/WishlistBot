using Serilog;
using WishlistBot.Queries;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages;

// TODO Combine common code with EditWish
[AllowedTypes(QueryParameterType.SetWishTo, QueryParameterType.ClaimWish)]
[ChildMessage(typeof(FullListMessage))]
public class ShowWishMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var sender = userContext.Users.First(u => u.UserId == userId);

        parameters.Peek(QueryParameterType.SetUserTo, out var targetId);
        var target = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == targetId);

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
                wish.ClaimerId = sender.UserId;
            }
            // Unclaim wish claimed by sender
            else if (wish.ClaimerId == sender.UserId)
            {
                wish.ClaimerId = null;
            }
            else
            {
                Logger.Error("ShowWish: parameters contain ClaimWish, but wish.ClaimerId is nor 0 neither [{senderId}], but [{claimerId}]", sender.UserId, wish.ClaimerId);
            }
        }

        if (wish.ClaimerId is not null)
        {
            var claimer = userContext.Users.FirstOrDefault(u => u.UserId == wish.ClaimerId);
            if (claimer is not null)
            {
                if (claimer.UserId == sender.UserId)
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

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var sender = user;
        user = Legacy_GetUser(user, parameters);

        parameters.Pop(QueryParameterType.SetWishTo, out var wishId);

        var wish = user.Wishes.FirstOrDefault(w => w.Id == wishId);
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
            if (wish.ClaimerId == 0)
            {
                wish.ClaimerId = sender.SenderId;
            }
            // Unclaim wish claimed by sender
            else if (wish.ClaimerId == sender.SenderId)
            {
                wish.ClaimerId = 0;
            }
            else
            {
                Logger.Error("ShowWish: parameters contain ClaimWish, but wish.ClaimerId is nor 0 neither [{senderId}], but [{claimerId}]", sender.SenderId, wish.ClaimerId);
            }
        }

        if (wish.ClaimerId != 0)
        {
            var claimer = Users.FirstOrDefault(u => u.SenderId == wish.ClaimerId);
            if (claimer is not null)
            {
                if (claimer.SenderId == sender.SenderId)
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
                Logger.Error("Wish [{wishId}]: Claimer [{claimerId}] not found in database. Cleaning ClaimerId", wish.Id, wish.ClaimerId);
                wish.ClaimerId = 0;
            }
        }

        // Checking ClaimerId in separate if because it can be reset when claimer was not found in database
        if (wish.ClaimerId == 0)
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
                var link = links[i];
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
