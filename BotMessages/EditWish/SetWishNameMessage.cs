using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Queries;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ForceNewWish)]
[MessageFor(typeof(SetWishNameQuery))]
public class SetWishNameMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .Include(u => u.Wishes)
            .First(u => u.UserId == userId);

        var forceNewWish = parameters.Pop(QueryParameterType.ForceNewWish);

        // Предел вишей проверяем здесь, а не только отсутствием кнопки в меню:
        // старое сообщение с кнопкой живёт долго, и кап через него не обойти
        if ((forceNewWish || user.CurrentWish is null) && WishLimits.IsFull(user.Wishes.Count))
        {
            Text
               .Bold($"В вишлисте уже {WishLimits.MaxWishes} вишей — это предел.")
               .BlockBreak()
               .Verbatim("Удалите что-нибудь, чтобы добавить новое.");

            user.BotState = BotState.Default;
            user.CurrentWish = null;

            Keyboard.AddButton<CompactListQuery>("К вишам");

            return Task.CompletedTask;
        }

        if (forceNewWish || user.CurrentWish is null)
        {
            if (user.CurrentWish is not null)
                user.CurrentWish = null;

            Text.Verbatim("Укажите краткое название виша:");
            Keyboard.AddButton<CompactListQuery>("Отмена");
        }
        else
        {
            Text
               .Bold("Текущее название виша: ")
               .Monospace(user.CurrentWish.Name);

            if (user.CurrentWish.Name.Length < MessageText.MaxCopyTextLength)
                Text.Verbatim(" ").CopyButton("Скопировать", user.CurrentWish.Name);

            Text.BlockBreak().Verbatim("Укажите новое название виша:");

            Keyboard.NewRow().AddButton<EditWishQuery>("Отмена");
        }

        user.BotState = BotState.ListenForWishName;

        return Task.CompletedTask;
    }
}
