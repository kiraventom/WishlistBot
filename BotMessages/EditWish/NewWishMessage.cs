using Serilog;
using WishlistBot.QueryParameters;
using WishlistBot.Queries;

namespace WishlistBot.BotMessages.EditWish;

/// <summary>
/// Черновик нового виша. Параметра <see cref="QueryParameterType.WishId"/> тут нет:
/// виша ещё не существует, поэтому окно всегда «добавление», а не «редактирование».
/// </summary>
[AllowedTypes(QueryParameterType.ClearWishProperty, QueryParameterType.SetPriceTo, QueryParameterType.ToggleWishPermanence, QueryParameterType.ShowHelp)]
[ChildMessage(typeof(CompactListMessage))]
public class NewWishMessage(ILogger logger) : WishDraftMessage(logger)
{
    protected override bool TryGetEditedWishId(QueryParameterCollection parameters, out long wishId)
    {
        wishId = 0;
        return false;
    }

    protected override void AddControls(bool isEditing, long wishId)
    {
        Keyboard.AddButton<ShowWishQuery>("Готово", QueryParameter.SaveDraft);
        Keyboard.AddButton<CompactListQuery>("Отмена");
    }
}
