using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Model;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin;

public class AdminMenuMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
    protected override Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Text.Bold("Admin menu");

        Keyboard
           .AddButton<BroadcastsQuery>()
           .NewRow()
           .AddButton("@admin_users", "Users")
           .NewRow()
           .AddButton("@admin_state", "Bot state");

        return Task.CompletedTask;
    }

    protected override Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Text.Bold("Admin menu");

        Keyboard
           .AddButton<BroadcastsQuery>()
           .NewRow()
           .AddButton("@admin_users", "Users")
           .NewRow()
           .AddButton("@admin_state", "Bot state");

        return Task.CompletedTask;
    }
}
