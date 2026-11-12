using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;

namespace WishlistBot.BotMessages.EditWish;

/// <summary>
/// Окно черновика виша: название, описание, картинка, ссылки, цена и тип (постоянный/обычный).
/// Новый виш и правка существующего показывают одно и то же, различаются подписью и нижними
/// кнопками, поэтому общая часть живёт здесь, а варианты — в <see cref="NewWishMessage"/>
/// и <see cref="EditWishMessage"/>.
/// </summary>
public abstract class WishDraftMessage(ILogger logger) : UserBotMessage(logger)
{
    /// <summary>
    /// Какой виш правим. Параметр <paramref name="wishId"/> заполняется, только если метод
    /// вернул true; иначе черновик считается новым вишем.
    /// </summary>
    protected abstract bool TryGetEditedWishId(QueryParameterCollection parameters, out long wishId);

    /// <summary>Нижние кнопки окна: у нового виша и у существующего они разные.</summary>
    protected abstract void AddControls(bool isEditing, long wishId);

    /// <summary>
    /// Строка параметра: подпись, значение (или «Не указано») и кнопка «Добавить»/«Изменить».
    /// Кнопка живёт в тексте, поэтому её callback-данные приходят из <c>AddTextButton</c>.
    /// </summary>
    private void AddField(string label, bool isSet, Action<MessageText> renderValue, string callbackData, bool blockValue = false)
    {
        Text.BlockBreak().Bold($"{label}: ");

        if (!isSet)
        {
            Text.Verbatim("Не указано ");
            Text.Button("Добавить", callbackData);
            return;
        }

        renderValue(Text);

        // Значение-блок (цитата, список) уже закончилось пустой строкой — кнопка идёт
        // отдельной строкой, без лишнего пробела перед ней
        if (!blockValue)
            Text.Verbatim(" ");

        Text.Button("Изменить", callbackData);
    }

    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .ThenInclude(w => w.Links)
            .Include(u => u.Wishes)
            .ThenInclude(w => w.Links)
            .First(u => u.UserId == userId);

        var isEditing = TryGetEditedWishId(parameters, out var wishId);

        if (isEditing)
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

        // Кнопки полей переехали в текст (tg-button рядом с каждым параметром),
        // в клавиатуре остались только «Готово» / «Отмена» / «Удалить»
        AddControls(isEditing, wishId);

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
                    wishDraft.MediaKey = null;
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

        Text.Heading(isEditing ? "Редактирование виша" : "Добавление виша");

        // Фото — медиа-блок в теле окна, а не отдельное сообщение с подписью
        Text.Media(wishDraft.MediaKey);

        // Все параметры показываются всегда: у пустого — «Не указано» и кнопка «Добавить»,
        // у заполненного — значение и кнопка «Изменить»
        AddField("Название", !string.IsNullOrEmpty(name),
            text => text.Monospace(name),
            Keyboard.AddTextButton<SetWishNameQuery>());

        AddField("Описание", description is not null,
            text => text.Quote(description),
            Keyboard.AddTextButton<SetWishDescriptionQuery>(),
            blockValue: true);

        AddField("Ссылки", links.Count != 0,
            text => text.Bullets(links.Count, (line, index) => line.InlineUrl(links[index].Url)),
            Keyboard.AddTextButton<SetWishLinksQuery>(),
            blockValue: true);

        AddField("Цена", priceRange != Price.NotSet,
            text => text.Monospace(MessageTextUtils.PriceToString(priceRange)),
            Keyboard.AddTextButton<SetWishPriceQuery>());

        AddField("Фото", wishDraft.MediaKey is not null,
            text => text.Verbatim("Приложено"),
            Keyboard.AddTextButton<SetWishMediaQuery>());

        Text.BlockBreak();

        if (wishDraft.IsPermanent)
        {
            Text.Verbatim("\u2b50 Постоянный виш ");
            Text.Button("Сделать обычным", Keyboard.AddTextButton<WishPermanenceQuery>());
        }
        else
        {
            Text.Verbatim("Обычный виш ");
            Text.Button("Сделать постоянным", Keyboard.AddTextButton<WishPermanenceQuery>());
        }

        return Task.CompletedTask;
    }
}
