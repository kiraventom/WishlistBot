using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Text;

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
            // короткий — обычным текстом, рядом с ним кнопка «Скопировать»
            if (linksJoined.Length >= MessageText.MaxCopyTextLength)
            {
                Text.Bullets(links.Count, (line, index) => line.Monospace(links[index].Url));
            }
            else
            {
                Text.Verbatim(" ").CopyButton("Скопировать", linksJoined);
                Text.Bullets(links.Count, (line, index) => line.Verbatim(links[index].Url));
            }

            // Убрать ссылки можно в окне виша («Удалить» у поля), здесь только правка
            Text.BlockBreak().Verbatim("Пришлите новые ссылки:");
        }

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        user.BotState = BotState.ListenForWishLinks;

        return Task.CompletedTask;
    }
}
