using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class SetWishLinksMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).ThenInclude(w => w.Links).First(u => u.UserId == userId);
        if (user.CurrentWish.Links.Any())
            Keyboard.AddButton<EditWishQuery>("Очистить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Links));

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        if (user.CurrentWish.Links is null)
        {
            Text.Verbatim("Пришлите ссылки одним сообщением:")
               .LineBreak()
               .LineBreak().Italic("(Некорректные ссылки будут проигнорированы)");
        }
        else
        {
            Text.Bold("Текущие ссылки: ");

            foreach (var link in user.CurrentWish.Links)
            {
                Text.LineBreak().Monospace(link.Url);
            }

            Text.LineBreak().LineBreak().Verbatim("Пришлите новые ссылки или удалите текущие:");
        }

        user.BotState = BotState.ListenForWishLinks;

        return Task.CompletedTask;
    }
}
