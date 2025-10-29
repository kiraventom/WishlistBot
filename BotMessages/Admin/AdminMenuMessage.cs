using Serilog;
using WishlistBot.Model.User;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Queries.Admin.Users;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin;

public class AdminMenuMessage(ILogger logger) : UserBotMessage(logger)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Bold("Admin menu");

        Keyboard
           .AddButton<BroadcastsQuery>()
           .NewRow()
           .AddButton<UsersQuery>()
           .NewRow()
           .AddButton("@admin_state", "Bot state");

        return Task.CompletedTask;
    }
}
