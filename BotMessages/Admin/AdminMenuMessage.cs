using Serilog;
using WishlistBot.Notification;
using WishlistBot.Database.Users;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Admin;
using WishlistBot.Queries.Admin.Broadcasts;
using WishlistBot.Database.Admin;
using WishlistBot.BotMessages.Admin.Broadcasts;

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
