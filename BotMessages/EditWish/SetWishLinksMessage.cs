using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[MessageFor(typeof(SetWishLinksQuery))]
public class SetWishLinksMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).ThenInclude(w => w.Links).First(u => u.UserId == userId);

        if (user.CurrentWish.Links.Count == 0)
        {
            Text.Verbatim("Пришлите ссылки одним сообщением:")
               .BlockBreak().Italic("(Некорректные ссылки будут проигнорированы)");
        }
        else
        {
            Text.Bold("Текущие ссылки:");

            var links = user.CurrentWish.Links;
            var linksJoined = string.Join("\n", links.Select(l => l.Url));

            // Длинный список идёт моноширинным (monospace сохраняет ссылку как есть),
            // короткий — обычным текстом, чтобы его можно было скопировать кнопкой
            if (linksJoined.Length >= 256)
                Text.Bullets(links.Count, (line, index) => line.Monospace(links[index].Url));
            else
            {
                Text.Bullets(links.Count, (line, index) => line.Verbatim(links[index].Url));
                Keyboard.AddCopyTextButton("Скопировать ссылки", linksJoined);
            }

            Text.BlockBreak().Verbatim("Пришлите новые ссылки или удалите текущие:");
        }

        if (user.CurrentWish.Links.Any())
            Keyboard
                .NewRow()
                .AddButton<EditWishQuery>("Очистить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Links));

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        user.BotState = BotState.ListenForWishLinks;

        return Task.CompletedTask;
    }
}
