using Telegram.Bot;
using Serilog;
using WishlistBot.Users;

namespace WishlistBot.Actions.Commands;

public abstract class Command : UserAction
{
   protected Command(ILogger logger, ITelegramBotClient client) : base(logger, client)
   {
   }
}
