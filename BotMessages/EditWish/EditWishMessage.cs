using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;

namespace WishlistBot.BotMessages.EditWish;

/// <summary>
/// Правка существующего виша: параметр <see cref="QueryParameterType.WishId"/> говорит,
/// какой именно. Без параметра окно ведёт себя как добавление — так в него возвращаются
/// кнопки «Отмена»/«Очистить» из подэкранов нового виша.
/// </summary>
[AllowedTypes(QueryParameterType.WishId, QueryParameterType.ClearWishProperty, QueryParameterType.SetPriceTo, QueryParameterType.ToggleWishPermanence)]
[ChildMessage(typeof(CompactListMessage))]
[MessageFor(typeof(EditWishQuery))]
[MessageFor(typeof(ConfirmWishPermanenceQuery))]
public class EditWishMessage(ILogger logger) : WishDraftMessage(logger)
{
    protected override bool TryGetEditedWishId(QueryParameterCollection parameters, out long wishId) =>
        parameters.Peek(QueryParameterType.WishId, out wishId);

    protected override void AddControls(bool isEditing, long wishId)
    {
        if (isEditing)
        {
            Keyboard.AddButton<ConfirmDeleteWishQuery>(new QueryParameter(QueryParameterType.WishId, wishId));
            Keyboard.NewRow();
            Keyboard.AddButton<ShowWishQuery>("Готово", QueryParameter.SaveDraft, new QueryParameter(QueryParameterType.WishId, wishId));
            Keyboard.AddButton<ShowWishQuery>("Отмена");
        }
        else
        {
            Keyboard.AddButton<ShowWishQuery>("Готово", QueryParameter.SaveDraft);
            Keyboard.AddButton<CompactListQuery>("Отмена");
        }
    }
}
