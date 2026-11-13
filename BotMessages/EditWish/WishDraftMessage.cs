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

    private const string EditCaption = "Изменить";
    private const string DeleteCaption = "Удалить";

    /// <summary>
    /// Строка параметра. Если значение есть — подпись, кнопки ✏️ и 🗑 справа от двоеточия,
    /// затем значение. Если нет — «Не указано» и одна кнопка («Указать» или «Добавить»).
    /// </summary>
    private void AddField(string label, bool isSet, Action<MessageText> renderValue, string editData,
        string deleteData = null, string emptyCaption = "Указать", bool valueOnSameLine = false)
    {
        Text.BlockBreak().Bold($"{label}: ");

        if (!isSet)
        {
            Text.Verbatim($"Не указано ").Button(emptyCaption, editData);
            return;
        }

        AddEditButtons(editData, deleteData);

        if (valueOnSameLine)
            Text.Verbatim(" ");

        renderValue(Text);
    }

    private void AddEditButtons(string editData, string deleteData)
    {
        Text.Button(EditCaption, editData);

        if (deleteData is not null)
            Text.Verbatim(" ").Button(DeleteCaption, deleteData);
    }

    private string DeleteData(WishPropertyType property) =>
        Keyboard.AddTextButton<EditWishQuery>(parameters: new QueryParameter(QueryParameterType.ClearWishProperty, (int)property));

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

        if (parameters.Pop(QueryParameterType.ShowHelp))
            AlertText = PermanentWishHelp;

        var name = wishDraft.Name;
        var description = wishDraft.Description;
        var links = wishDraft.Links;
        var priceRange = wishDraft.PriceRange;

        Text.Heading(isEditing ? "Редактирование виша" : "Добавление виша");

        // Фото — медиа-блок в теле окна, а не отдельное сообщение с подписью
        Text.Media(wishDraft.MediaKey);

        // Фото идёт сразу под картинкой, до остальных полей
        AddField("Фото", wishDraft.MediaKey is not null,
            _ => { },
            Keyboard.AddTextButton<SetWishMediaQuery>(),
            DeleteData(WishPropertyType.Media),
            emptyCaption: "Добавить");

        // Название: значение, а за ним — кнопки «максимально справа»
        Text.BlockBreak().Bold("Название: ").Monospace(name).Verbatim(" ");
        AddEditButtons(Keyboard.AddTextButton<SetWishNameQuery>(), null);

        // Описание и ссылки-блоки: кнопки справа от двоеточия, значение — следующей строкой
        AddField("Описание", description is not null,
            text => text.Quote(description),
            Keyboard.AddTextButton<SetWishDescriptionQuery>(),
            DeleteData(WishPropertyType.Description));

        if (links.Count == 1)
        {
            // Одна ссылка — всё в одну строку: «Ссылка: <ссылка> ✏️ 🗑»
            Text.BlockBreak().Bold("Ссылка: ").InlineUrl(links[0].Url);
            Text.Verbatim(" ");
            AddEditButtons(Keyboard.AddTextButton<SetWishLinksQuery>(), DeleteData(WishPropertyType.Links));
        }
        else
        {
            AddField("Ссылки", links.Count != 0,
                text => text.Bullets(links.Count, (line, index) => line.InlineUrl(links[index].Url)),
                Keyboard.AddTextButton<SetWishLinksQuery>(),
                links.Count != 0 ? DeleteData(WishPropertyType.Links) : null,
                emptyCaption: "Добавить");
        }

        // Цена: значение, затем кнопки — как у названия
        Text.BlockBreak().Bold("Цена: ");

        if (priceRange == Price.NotSet)
        {
            Text.Verbatim("Не указано ").Button("Указать", Keyboard.AddTextButton<SetWishPriceQuery>());
        }
        else
        {
            Text.Monospace(MessageTextUtils.PriceToString(priceRange)).Verbatim(" ");
            AddEditButtons(Keyboard.AddTextButton<SetWishPriceQuery>(), DeleteData(WishPropertyType.Price));
        }

        Text.BlockBreak().Bold("Тип виша: ");

        if (wishDraft.IsPermanent)
        {
            Text.Verbatim("\u2b50 постоянный ").Button("Сделать обычным",
                Keyboard.AddTextButton<EditWishQuery>(parameters: QueryParameter.ToggleWishPermanence));
        }
        else
        {
            Text.Verbatim("обычный ").Button("Сделать постоянным",
                Keyboard.AddTextButton<EditWishQuery>(parameters: QueryParameter.ToggleWishPermanence));

            Text.Verbatim(" ").Button("Что это?",
                Keyboard.AddTextButton<EditWishQuery>(parameters: QueryParameter.ShowHelp), style: "primary");
        }

        return Task.CompletedTask;
    }
}
