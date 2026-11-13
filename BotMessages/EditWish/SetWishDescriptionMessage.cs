using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Text;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[MessageFor(typeof(SetWishDescriptionQuery))]
public class SetWishDescriptionMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);

        if (user.CurrentWish.Description is null)
        {
            Text.Verbatim("Укажите подробное описание виша:");
        }
        else
        {
            Text.Bold("Текущее описание виша:");

            if (user.CurrentWish.Description.Length < MessageText.MaxCopyTextLength)
                Text.Verbatim(" ").CopyButton("Скопировать", user.CurrentWish.Description);

            if (user.CurrentWish.Description.Length >= MessageText.MaxCopyTextLength)
            {
                Text.BlockBreak().Monospace(user.CurrentWish.Description);
            }
            else
            {
                Text.ExpandableQuote(user.CurrentWish.Description);
            }

            // Убрать описание можно в окне виша («Удалить» у поля), здесь только правка
            Text.BlockBreak().Verbatim("Укажите новое описание:");
        }

        Keyboard
           .NewRow()
           .AddButton<EditWishQuery>("Отмена");

        user.BotState = BotState.ListenForWishDescription;

        return Task.CompletedTask;
    }
}
