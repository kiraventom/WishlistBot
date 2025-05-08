using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetWishTo, QueryParameterType.ClearWishProperty, QueryParameterType.SetPriceTo)]
[ChildMessage(typeof(FullListMessage))]
// TODO Separate to NewWish and EditWish
public class EditWishMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<SetWishNameQuery>()
           .AddButton<SetWishDescriptionQuery>()
           .NewRow()
           .AddButton<SetWishMediaQuery>()
           .AddButton<SetWishLinksQuery>()
           .NewRow()
           .AddButton<SetWishPriceQuery>()
           .NewRow();

        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .ThenInclude(w => w.Links)
            .Include(u => u.Wishes)
            .ThenInclude(w => w.Links)
            .First(u => u.UserId == userId);

        if (parameters.Peek(QueryParameterType.ReturnToFullList))
        {
            parameters.Peek(QueryParameterType.SetWishTo, out var wishId);

            if (user.CurrentWish is null)
            {
                Logger.Debug("Parameter SetWishTo={setWishTo}", wishId);
                Logger.Debug("Current user wish ids: [ {wishIds} ]", string.Join(", ", user.Wishes.Select(w => w.WishId)));

                var wishToEdit = user.Wishes.FirstOrDefault(w => w.WishId == wishId);
                if (wishToEdit is null)
                {
                    throw new NotSupportedException($"Can't find wish {wishId} to edit");
                }

                if (user.Wishes.Count(w => w.WishId == wishId) > 1)
                {
                    throw new NotSupportedException($"There are more than one wish with id {wishId}");
                }

                Logger.Debug("WishToEdit ID={wishToEdit}", wishToEdit.WishId);

                var draft = WishDraftModel.FromWish(wishToEdit);

                userContext.WishDrafts.Add(draft);
                userContext.SaveChanges();
            }

            Keyboard.AddButton<ConfirmDeleteWishQuery>(new QueryParameter(QueryParameterType.SetWishTo, wishId));
            Keyboard.NewRow();
            Keyboard.AddButton<FinishEditWishQuery>(new QueryParameter(QueryParameterType.SetWishTo, wishId));
        }
        else
        {
            Keyboard.AddButton<FinishEditWishQuery>();
        }

        Keyboard.AddButton<CancelEditWishQuery>();

        var wishDraft = user.CurrentWish;

        if (parameters.Pop(QueryParameterType.SetPriceTo, out var priceValue))
        {
            var price = (Price)priceValue;
            wishDraft.PriceRange = price;
        }

        if (parameters.Pop(QueryParameterType.ClearWishProperty, out var propertyTypeValue))
        {
            var propertyType = (WishPropertyType)propertyTypeValue;
            switch (propertyType)
            {
                case WishPropertyType.Price:
                    wishDraft.PriceRange = Price.NotSet;
                    break;
                case WishPropertyType.Description:
                    wishDraft.Description = null;
                    break;
                case WishPropertyType.Media:
                    wishDraft.FileId = null;
                    break;
                case WishPropertyType.Links:
                    wishDraft.Links.Clear();
                    break;
            }
        }

        var name = wishDraft.Name;
        var description = wishDraft.Description;
        var links = wishDraft.Links;
        var priceRange = wishDraft.PriceRange;

        //if (wish.ClaimerId != 0)
        //{
        //   Text.ItalicBold("\u203c\ufe0f Будьте осторожны! Кто-то забронировал этот виш! \u203c\ufe0f").LineBreak().LineBreak();
        //}

        Text.Italic("Редактирование виша")
           .LineBreak()
           .LineBreak().Bold("Название: ").Monospace(name);

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

        PhotoFileId = wishDraft.FileId;

        return Task.CompletedTask;
    }
}
