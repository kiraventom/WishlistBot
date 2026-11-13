using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[MessageFor(typeof(SetWishMediaQuery))]
public class SetWishMediaMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);
        if (user.CurrentWish.MediaKey is not null)
            Keyboard.AddButton<EditWishQuery>("Удалить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Media));

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        Text.Media(user.CurrentWish.MediaKey);

        Text.Verbatim(user.CurrentWish.MediaKey is not null ? "Пришлите новое фото или удалите текущее" : "Пришлите фото виша:");

        user.BotState = BotState.ListenForWishMedia;

        return Task.CompletedTask;
    }
}
