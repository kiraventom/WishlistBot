using Serilog;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.SetUserTo)]
public abstract class UserBotMessage(ILogger logger) : BotMessage(logger)
{
}

