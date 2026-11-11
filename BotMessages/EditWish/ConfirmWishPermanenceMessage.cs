using Serilog;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Model.User;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.EditWish;

/// <summary>
/// Подтверждение смены типа виша. Для превращения в постоянный объясняем, что это такое,
/// для обратного превращения — просто вопрос.
/// </summary>
[ChildMessage(typeof(EditWishMessage))]
public class ConfirmWishPermanenceMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .AsNoTracking()
            .First(u => u.UserId == userId);

        var wishDraft = user.CurrentWish;
        var willBePermanent = !wishDraft.IsPermanent;

        if (willBePermanent)
        {
            Text.Verbatim("Постоянный виш — это подарок, которому вы рады всегда!")
               .LineBreak().LineBreak()
               .Verbatim("К примеру, постоянным вишем может быть испанское полусладкое вино, хорошая книга, сертификат в Золотое Яблоко.")
               .LineBreak()
               .Verbatim("Постоянный виш нельзя забронировать, поэтому любой из ваших друзей сможет порадовать вас им!")
               .LineBreak().LineBreak();
        }

        Text.Verbatim("Сделать виш ")
           .Monospace(wishDraft.Name)
           .Verbatim(willBePermanent ? " постоянным?" : " обычным?");

        Keyboard
           .AddButton<ConfirmWishPermanenceQuery>(null, QueryParameter.ToggleWishPermanence)
           .AddButton<EditWishQuery>("\u274c Нет");

        return Task.CompletedTask;
    }
}
