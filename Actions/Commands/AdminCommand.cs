using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages.Admin;
using WishlistBot.Model.User;

namespace WishlistBot.Actions.Commands;

public class AdminCommand(ILogger logger, BotMessageSender sender) : Command(logger, sender)
{
   public override string Name => "/admin";

   public override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
   {
      if (user.IsAdmin)
      {
         await Sender.SendOrEdit(userContext, user.UserId, new AdminMenuMessage(Logger), forceNewMessage: true);
      }
      else
      {
         Logger.Warning("{command} called, but [{id}] is not admin", Name, user.UserId);
      }
   }
}
