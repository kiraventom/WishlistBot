using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Subscription;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using WishlistBot.BotMessages.EditWish;

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
        // TODO
        else if (TryParseShowWishAction(actionText, out var userId, out var wishId, out var pageIndex))
        {
            var collection = new QueryParameterCollection(
            [
                new QueryParameter(QueryParameterType.SetUserTo, userId), 
                new QueryParameter(QueryParameterType.SetWishTo, wishId),
                new QueryParameter(QueryParameterType.SetListPageTo, pageIndex)
            ]);

            user.QueryParams = collection.ToString();
            
            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new ShowWishMessage(Logger));
        }
        else
        {
            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new MainMenuMessage(Logger), forceNewMessage: true);
        }
    }

    public override bool ShouldCleanup(string actionText) => TryParseShowWishAction(actionText, out _, out _, out _);

    private static bool TryParseSubscribeId(string actionText, out string subscribeId)
    {
        var parts = actionText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && Guid.TryParse(parts[1], out _))
        {
            subscribeId = parts[1];
            return true;
        }

        subscribeId = null;
        return false;
    }

    private static bool TryParseShowWishAction(string actionText, out int userId, out int wishId, out int pageIndex)
    {
        userId = -1;
        wishId = -1;
        pageIndex = -1;

        var parts = actionText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            Dictionary<string, string> parameters = new();

            var parametersTxt = parts[1].Split('_');
            foreach (var parameter in parametersTxt)
            {
                var parameterKeyValue = parameter.Split('=');
                if (parameterKeyValue.Length != 2)
                    return false;

                parameters.Add(parameterKeyValue[0], parameterKeyValue[1]);
            }

            if (parameters["action"] == "showwish")
            {
                userId = int.Parse(parameters["setuserto"]);
                wishId = int.Parse(parameters["setwishto"]);
                pageIndex = int.Parse(parameters["setlistpageto"]);
                return true;
            }
        }

        return false;
    }
}

