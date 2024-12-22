using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Subscription;
using WishlistBot.Queries.Parameters;

namespace WishlistBot.Actions.Commands;

public class AdminCommand(ILogger logger, ITelegramBotClient client, long adminId) : Command(logger, client)
{
   public override string Name => "/admin";

   public override async Task ExecuteAsync(BotUser user, string actionText)
   {
   }
}
