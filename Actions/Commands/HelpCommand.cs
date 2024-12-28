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

public class HelpCommand(ILogger logger, ITelegramBotClient client) : Command(logger, client)
{
   public override string Name => "/help";

   public override async Task ExecuteAsync(BotUser user, string actionText)
   {
      await Client.SendOrEditBotMessage(Logger, user, new HelpMessage(Logger), forceNewMessage: true);
   }
}
