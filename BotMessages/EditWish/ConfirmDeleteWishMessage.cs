using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class ConfirmDeleteWishMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<DeleteWishQuery>()
           .NewRow()
           .AddButton<EditWishQuery>("Отмена \u274c");

        var user = userContext.Users.Include(u => u.CurrentWish).AsNoTracking().First(u => u.UserId == userId);
        if (user.CurrentWish.ClaimerId is not null)
        {
            Text.ItalicBold("\u203c\ufe0f Будьте осторожны! Кто-то забронировал этот виш! \u203c\ufe0f").LineBreak().LineBreak();
        }

        Text.Italic("Действительно удалить виш \"")
           .Monospace(user.CurrentWish.Name)
           .Italic("\"")
           .ItalicBold(" навсегда")
           .Italic("?");

        return Task.CompletedTask;
    }
}
