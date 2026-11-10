using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.BotMessages.Admin.Users;
using WishlistBot.BotMessages.Admin.Broadcasts;

namespace WishlistBot.Actions.Commands;

public class StartCommand(ILogger logger, ITelegramBotClient client) : Command(logger, client)
{
    public override string Name => "/start";

    public override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
    {
        user.QueryParams = null;

        userContext.Entry(user).Reference(u => u.CurrentWish).Load();
        user.CurrentWish = null;

        var isInviteLink = TryParseSubscribeId(actionText, out var subscribeId);
        if (isInviteLink)
        {
            var userToAddAsFriend = userContext.Users.FirstOrDefault(u => u.SubscribeId == subscribeId);

            if (userToAddAsFriend is null)
            {
                await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new FailFriendLinkMessage(Logger), forceNewMessage: true);
                return;
            }

            if (userToAddAsFriend == user)
            {
                await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new MainMenuMessage(Logger), forceNewMessage: true);
                return;
            }

            var collection = new QueryParameterCollection([new QueryParameter(QueryParameterType.UserId, userToAddAsFriend.UserId)]);
            user.QueryParams = collection.ToString();
            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new AddFriendMessage(Logger), forceNewMessage: true);
        }
        // TODO
        else if (TryParseShowWishAction(actionText, out var userSubscribeId, out var wishId, out var returnToMyClaims)
                 && userContext.Users.FirstOrDefault(u => u.SubscribeId.StartsWith(userSubscribeId)) is {} wishOwner)
        {
            var queryParameters = new List<QueryParameter>
            {
                new(QueryParameterType.UserId, wishOwner.UserId),
                new(QueryParameterType.WishId, wishId),
            };

            // Ссылка из списка броней должна вернуть обратно в этот список
            if (returnToMyClaims)
                queryParameters.Add(QueryParameter.ReturnToMyClaims);

            user.QueryParams = new QueryParameterCollection(queryParameters).ToString();

            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new ShowWishMessage(Logger));
        }
        else if (TryParseOpenFriendAction(actionText, out var friendSubscribeId)
                 && userContext.Users.FirstOrDefault(u => u.SubscribeId.StartsWith(friendSubscribeId)) is {} friend)
        {
            var collection = new QueryParameterCollection([new QueryParameter(QueryParameterType.UserId, friend.UserId)]);
            user.QueryParams = collection.ToString();

            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new FriendMessage(Logger));
        }
        else if (TryParseOpenBroadcastAction(actionText, out var broadcastId))
        {
            if (!user.IsAdmin)
            {
                Logger.Warning("{actionText} called, but [{id}] is not admin", actionText, user.UserId);
                return;
            }

            var collection = new QueryParameterCollection([new QueryParameter(QueryParameterType.SetBroadcastTo, broadcastId)]);
            user.QueryParams = collection.ToString();

            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new BroadcastMessage(Logger));
        }
        else if (TryParseAdminShowUserAction(actionText, out var userId1))
        {
            if (!user.IsAdmin)
            {
                Logger.Warning("{actionText} called, but [{id}] is not admin", actionText, user.UserId);
                return;
            }

            var collection = new QueryParameterCollection(
            [
                new QueryParameter(QueryParameterType.UserId, userId1), 
            ]);

            user.QueryParams = collection.ToString();
            
            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new UserMessage(Logger));
        }
        else
        {
            await Client.SendOrEditBotMessage(Logger, userContext, user.UserId, new MainMenuMessage(Logger), forceNewMessage: true);
        }
    }

    public override bool ShouldCleanup(string actionText) =>
        TryParseShowWishAction(actionText, out _, out _, out _)
        || TryParseAdminShowUserAction(actionText, out _)
        || TryParseOpenFriendAction(actionText, out _)
        || TryParseOpenBroadcastAction(actionText, out _);

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

    /// <summary>
    /// Разбирает хвост диплинка вида <c>a=sw_u=xxxx_w=42</c> в словарь параметров.
    /// </summary>
    private static bool TryParseLinkParameters(string actionText, out Dictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>();

        var parts = actionText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        foreach (var parameter in parts[1].Split('_'))
        {
            var parameterKeyValue = parameter.Split('=');
            if (parameterKeyValue.Length != 2)
                return false;

            if (!parameters.TryAdd(parameterKeyValue[0], parameterKeyValue[1]))
                return false;
        }

        return parameters.ContainsKey("a");
    }

    private static bool TryParseShowWishAction(string actionText, out string userSubcribeId, out int wishId, out bool returnToMyClaims)
    {
        userSubcribeId = null;
        wishId = -1;
        returnToMyClaims = false;

        if (!TryParseLinkParameters(actionText, out var parameters) || parameters["a"] != "sw")
            return false;

        returnToMyClaims = parameters.TryGetValue("c", out var claimsFlag) && claimsFlag == "1";

        return parameters.TryGetValue("u", out userSubcribeId)
            && parameters.TryGetValue("w", out var wishIdStr)
            && int.TryParse(wishIdStr, out wishId);
    }

    private static bool TryParseOpenFriendAction(string actionText, out string friendSubscribeId)
    {
        friendSubscribeId = null;

        if (!TryParseLinkParameters(actionText, out var parameters) || parameters["a"] != "fr")
            return false;

        return parameters.TryGetValue("u", out friendSubscribeId);
    }

    private static bool TryParseOpenBroadcastAction(string actionText, out int broadcastId)
    {
        broadcastId = -1;

        if (!TryParseLinkParameters(actionText, out var parameters) || parameters["a"] != "br")
            return false;

        return parameters.TryGetValue("i", out var broadcastIdStr) && int.TryParse(broadcastIdStr, out broadcastId);
    }

    private static bool TryParseAdminShowUserAction(string actionText, out int userId)
    {
        userId = -1;

        if (!TryParseLinkParameters(actionText, out var parameters) || parameters["a"] != "asu")
            return false;

        return parameters.TryGetValue("u", out var userIdStr) && int.TryParse(userIdStr, out userId);
    }
}

