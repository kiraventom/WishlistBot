using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.Model;

namespace WishlistBot.Actions.Commands;

public class HelpCommand(ILogger logger, ITelegramBotClient client) : Command(logger, client)
{
   public override string Name => "/help";

   public override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
   {
      await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new HelpMessage(Logger), forceNewMessage: true);
   }
}
