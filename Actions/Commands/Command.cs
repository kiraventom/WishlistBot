using Telegram.Bot;
using Serilog;

namespace WishlistBot.Actions.Commands;

public abstract class Command : UserAction
{
   protected Command(ILogger logger, ITelegramBotClient client) : base(logger, client)
   {
   }
}
