using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;

namespace WishlistBot.Actions.Commands;

public class HelpCommand(ILogger logger, ITelegramBotClient client) : Command(logger, client)
{
   public override string Name => "/help";

   public override async Task ExecuteAsync(BotUser user, string actionText)
   {
      await Client.SendOrEditBotMessage(Logger, user, new HelpMessage(Logger), forceNewMessage: true);
   }
}
