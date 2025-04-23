using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ForceNewWish)]
public class SetWishNameMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);
        var forceNewWish = parameters.Pop(QueryParameterType.ForceNewWish);

        if (forceNewWish || user.CurrentWish is null)
        {
            if (user.CurrentWish is not null)
                user.CurrentWish = null;

            Text.Verbatim("Укажите краткое название виша:");
            Keyboard.AddButton<CancelEditWishQuery>();
        }
        else
        {
            Text
               .Bold("Текущее название виша: ")
               .Monospace(user.CurrentWish.Name)
               .LineBreak()
               .LineBreak().Verbatim("Укажите новое название виша:");

            Keyboard.AddButton<EditWishQuery>("Отмена");
        }

        user.BotState = BotState.ListenForWishName;

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        var forceNewWish = parameters.Pop(QueryParameterType.ForceNewWish);

        if (forceNewWish || user.CurrentWish is null)
        {
            user.CurrentWish = new Wish()
            {
                Id = GenerateWishId()
            };
            Text.Verbatim("Укажите краткое название виша:");
            Keyboard.AddButton<CancelEditWishQuery>();
        }
        else
        {
            Text
               .Bold("Текущее название виша: ")
               .Monospace(user.CurrentWish.Name)
               .LineBreak()
               .LineBreak().Verbatim("Укажите новое название виша:");

            Keyboard.AddButton<EditWishQuery>("Отмена");
        }

        user.BotState = BotState.ListenForWishName;

        return Task.CompletedTask;
    }
}
