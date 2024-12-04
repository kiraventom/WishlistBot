using Telegram.Bot;
using Serilog;

namespace WishlistBot.Actions.Commands;

public abstract class Command(ILogger logger, ITelegramBotClient client) : UserAction(logger, client);
