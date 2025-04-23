using Microsoft.EntityFrameworkCore;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Model;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ReturnToFullList)]
public class CancelEditWishMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        if (parameters.Pop(QueryParameterType.ReturnToFullList))
        {
            Text.Italic("Редактирование виша отменено");
            Keyboard.AddButton<FullListQuery>("Назад к списку");
        }
        else
        {
            Text.Italic("Создание виша отменено");
            Keyboard.AddButton<SetWishNameQuery>("Добавить другой виш")
               .NewRow()
               .AddButton<CompactListQuery>("Назад к моим вишам");
        }

        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);
        userContext.WishDrafts.Remove(user.CurrentWish);
        user.CurrentWish = null;

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        if (parameters.Pop(QueryParameterType.ReturnToFullList))
        {
            Text.Italic("Редактирование виша отменено");
            Keyboard.AddButton<FullListQuery>("Назад к списку");
        }
        else
        {
            Text.Italic("Создание виша отменено");
            Keyboard.AddButton<SetWishNameQuery>("Добавить другой виш")
               .NewRow()
               .AddButton<CompactListQuery>("Назад к моим вишам");
        }

        user.CurrentWish = null;

        return Task.CompletedTask;
    }
}
