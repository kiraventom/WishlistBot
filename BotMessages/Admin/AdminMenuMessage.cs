using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Admin;

public class AdminMenuMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
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
