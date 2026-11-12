using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.Model.User;

namespace WishlistBot.Actions.Commands;

public class HelpCommand(ILogger logger, BotMessageSender sender) : Command(logger, sender)
{
   public override string Name => "/help";

   public override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
   {
      await Sender.SendOrEdit(userContext, user.UserId, new HelpMessage(Logger), forceNewMessage: true);
   }
}
