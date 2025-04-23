using Serilog;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.QueryParameters;
using WishlistBot.Text;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class SetWishPriceMessage(ILogger logger) : BotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);
        foreach (var price in Price.GetValues<Price>())
        {
            Keyboard
               .NewRow()
               .AddButton<EditWishQuery>(MessageTextUtils.PriceToString(price), new QueryParameter(QueryParameterType.SetPriceTo, (int)price));
        }

        if (user.CurrentWish.PriceRange != Price.NotSet)
            Text.Bold("Текущая цена: ").Monospace(MessageTextUtils.PriceToString(user.CurrentWish.PriceRange)).LineBreak();

        Text.Verbatim("Укажите примерную цену виша:");
        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        foreach (var price in Price.GetValues<Price>())
        {
            Keyboard
               .NewRow()
               .AddButton<EditWishQuery>(MessageTextUtils.PriceToString(price), new QueryParameter(QueryParameterType.SetPriceTo, (int)price));
        }

        if (user.CurrentWish.PriceRange != Price.NotSet)
            Text.Bold("Текущая цена: ").Monospace(MessageTextUtils.PriceToString(user.CurrentWish.PriceRange)).LineBreak();

        Text.Verbatim("Укажите примерную цену виша:");
        return Task.CompletedTask;
    }
}
