using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;

using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Subscription;
using WishlistBot.Queries.Parameters;
using WishlistBot.BotMessages.Admin;
using WishlistBot.BotMessages.Admin.Broadcasts;

namespace WishlistBot.Actions.Commands;

public class AdminCommand(ILogger logger, ITelegramBotClient client, UsersDb usersDb, long adminId) : Command(logger, client)
{
   public override string Name => "/admin";

   public override async Task ExecuteAsync(BotUser user, string actionText)
   {
      if (user.SenderId == adminId)
         await Client.SendOrEditBotMessage(Logger, user, new AdminMenuMessage(Logger, usersDb), forceNewMessage: true);
      else
         Logger.Warning("{command} called, but [{id}] is not admin", Name, user.SenderId);
   }
}
