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

        if (user.CurrentWish.Links.Count == 0)
        {
            Text.Verbatim("Пришлите ссылки одним сообщением:")
               .LineBreak()
               .LineBreak().Italic("(Некорректные ссылки будут проигнорированы)");
        }
        else
        {
            Text.Bold("Текущие ссылки: ");

            var linksJoined = string.Join("\n", user.CurrentWish.Links.Select(l => l.Url));
            if (linksJoined.Length >= 256)
            {
                foreach (var link in user.CurrentWish.Links)
                {
                    Text.LineBreak().Monospace(link.Url);
                }
            }
            else
            {
                foreach (var link in user.CurrentWish.Links)
                {
                    Text.LineBreak().Verbatim(link.Url);
                }

                Keyboard.AddCopyTextButton("Скопировать ссылки", linksJoined);
            }

            Text.LineBreak().LineBreak().Verbatim("Пришлите новые ссылки или удалите текущие:");
        }

        if (user.CurrentWish.Links.Any())
            Keyboard.AddButton<EditWishQuery>("Очистить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Links));

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        user.BotState = BotState.ListenForWishLinks;

        return Task.CompletedTask;
    }
}
