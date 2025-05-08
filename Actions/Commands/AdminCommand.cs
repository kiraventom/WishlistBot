using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages.Admin;
using WishlistBot.Model;

namespace WishlistBot.Actions.Commands;

public class AdminCommand(ILogger logger, ITelegramBotClient client) : Command(logger, client)
{
   public override string Name => "/admin";

   public override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
   {
      if (user.IsAdmin)
      {
         await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new AdminMenuMessage(Logger), forceNewMessage: true);
      }
      else
      {
         Logger.Warning("{command} called, but [{id}] is not admin", Name, user.UserId);
      }
   }
}
