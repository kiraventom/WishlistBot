using Serilog;

namespace WishlistBot.Actions.Commands;

public abstract class Command(ILogger logger, BotMessageSender sender) : UserAction(logger, sender);
