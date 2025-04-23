using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class SetWishMediaMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);
        if (user.CurrentWish.FileId is not null)
            Keyboard.AddButton<EditWishQuery>("Удалить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Media));

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        PhotoFileId = user.CurrentWish.FileId;

        Text.Verbatim(PhotoFileId is not null ? "Пришлите новое фото или удалите текущее" : "Пришлите фото виша:");

        user.BotState = BotState.ListenForWishMedia;

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        if (user.CurrentWish.FileId is not null)
            Keyboard.AddButton<EditWishQuery>("Удалить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Media));

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        PhotoFileId = user.CurrentWish.FileId;

        Text.Verbatim(PhotoFileId is not null ? "Пришлите новое фото или удалите текущее" : "Пришлите фото виша:");

        user.BotState = BotState.ListenForWishMedia;

        return Task.CompletedTask;
    }
}
