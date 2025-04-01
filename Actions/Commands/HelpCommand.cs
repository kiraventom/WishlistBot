using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;
using WishlistBot.Model;

namespace WishlistBot.Actions.Commands;

public class HelpCommand(ILogger logger, ITelegramBotClient client) : Command(logger, client)
{
   public override string Name => "/help";

   public override async Task ExecuteAsync(UserContext userContext, UserModel userModel, string actionText)
   {
      await Client.SendOrEditBotMessage(Logger, userContext, userModel, new HelpMessage(Logger), forceNewMessage: true);
   }

   public override async Task Legacy_ExecuteAsync(BotUser user, string actionText)
   {
      await Client.Legacy_SendOrEditBotMessage(Logger, user, new HelpMessage(Logger), forceNewMessage: true);
   }
}
