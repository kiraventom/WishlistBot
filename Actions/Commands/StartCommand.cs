using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;

namespace WishlistBot.Actions.Commands;

public class StartCommand(ILogger logger, ITelegramBotClient client) : Command(logger, client)
{
    public override string Name => "/start";

    public override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
    {
        user.QueryParams = null;

        var isSubscribe = TryParseSubscribeId(actionText, out var subscribeId);
        if (isSubscribe)
        {
            var userToSubscribeTo = userContext.Users.FirstOrDefault(u => u.SubscribeId == subscribeId);

            if (userToSubscribeTo is null)
            {
                await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new FailSubscriptionMessage(Logger), forceNewMessage: true);
                return;
            }

            if (userToSubscribeTo == user)
            {
                await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new MainMenuMessage(Logger), forceNewMessage: true);
                return;
            }

            var collection = new QueryParameterCollection([new QueryParameter(QueryParameterType.SetUserTo, userToSubscribeTo.UserId)]);
            user.QueryParams = collection.ToString();
            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new FinishSubscriptionMessage(Logger), forceNewMessage: true);
        }
        else
        {
            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new MainMenuMessage(Logger), forceNewMessage: true);
        }
    }

    private static bool TryParseSubscribeId(string actionText, out string subscribeId)
    {
        var parts = actionText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            subscribeId = parts[1];
            return true;
        }

        subscribeId = null;
        return false;
    }
}

