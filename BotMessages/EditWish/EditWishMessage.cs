using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries;

namespace WishlistBot.BotMessages.EditWish;

[AllowedTypes(QueryParameterType.WishId, QueryParameterType.ClearWishProperty, QueryParameterType.SetPriceTo, QueryParameterType.ToggleWishPermanence)]
[ChildMessage(typeof(CompactListMessage))]
// TODO Separate to NewWish and EditWish
public class EditWishMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .ThenInclude(w => w.Links)
            .Include(u => u.Wishes)
            .ThenInclude(w => w.Links)
            .First(u => u.UserId == userId);

        var isEditing = parameters.Peek(QueryParameterType.WishId, out var wishId);
        if (isEditing) // Editing
        {
            // Черновик принадлежит конкретному вишу: если остался чужой (прерванное
            // редактирование другого виша или новая правка после уведомления),
            // его выбрасываем, иначе правки уедут не в тот виш.
            if (user.CurrentWish is null || user.CurrentWish.OriginalId != wishId)
            {
                if (user.CurrentWish is not null)
                    userContext.WishDrafts.Remove(user.CurrentWish);

                var wish = user.Wishes.First(w => w.WishId == wishId);
                user.CurrentWish = WishDraftModel.FromWish(wish);
            }
        }

        var wishDraft = user.CurrentWish;

        // Тип виша переключается отдельным экраном подтверждения
        if (parameters.Pop(QueryParameterType.ToggleWishPermanence))
            wishDraft.IsPermanent = !wishDraft.IsPermanent;

        Keyboard
           .AddButton<SetWishNameQuery>()
           .AddButton<SetWishDescriptionQuery>()
           .NewRow()
           .AddButton<SetWishMediaQuery>()
           .AddButton<SetWishLinksQuery>()
           .NewRow()
           .AddButton<SetWishPriceQuery>()
           .NewRow()
           .AddButton<WishPermanenceQuery>(wishDraft.IsPermanent ? "Сделать обычным" : null)
           .NewRow();

        if (isEditing)
        {
            Keyboard.AddButton<ConfirmDeleteWishQuery>(new QueryParameter(QueryParameterType.WishId, wishId));
            Keyboard.NewRow();
            Keyboard.AddButton<ShowWishQuery>("Готово", QueryParameter.SaveDraft, new QueryParameter(QueryParameterType.WishId, wishId));
            Keyboard.AddButton<ShowWishQuery>("Отмена");
        }
        else // Adding
        {
            Keyboard.AddButton<ShowWishQuery>("Готово", QueryParameter.SaveDraft);
            Keyboard.AddButton<CompactListQuery>("Отмена");
        }

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

        if (isEditing)
            Text.Italic("Редактирование виша");
        else
            Text.Italic("Добавление виша");

        Text
           .LineBreak()
           .LineBreak().Bold("Название: ").Monospace(name);

        if (description is not null)
            Text.LineBreak().Bold("Описание: ").LineBreak().Quote(description);

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

        if (wishDraft.IsPermanent)
            Text.LineBreak().Verbatim("\u2b50 Постоянный виш");

        PhotoFileId = wishDraft.FileId;

        return Task.CompletedTask;
    }
}
