using Telegram.Bot;
using Serilog;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.Friends;
using WishlistBot.QueryParameters;
using WishlistBot.Model.User;
using WishlistBot.BotMessages.Admin.Users;
using WishlistBot.BotMessages.Admin.Broadcasts;
using WishlistBot.BotMessages.Settings;
using WishlistBot.BotMessages.Profile;

namespace WishlistBot.Actions.Commands;

public class StartCommand(ILogger logger, BotMessageSender sender) : Command(logger, sender)
{
    public override string Name => "/start";

    public override async Task ExecuteAsync(UserContext userContext, UserModel user, string actionText)
    {
        user.QueryParams = null;

        userContext.Entry(user).Reference(u => u.CurrentWish).Load();
        user.CurrentWish = null;

        if (TryParseInviteLink(actionText, out var subscribeId))
        {
            await HandleInviteLink(userContext, user, subscribeId);
            return;
        }

        if (DeepLink.TryParse(actionText, out var kind, out var parameters))
        {
            var handled = kind switch
            {
                DeepLink.Kind.Wish => await HandleWishLink(userContext, user, parameters),
                DeepLink.Kind.Friend => await HandleFriendLink(userContext, user, parameters),
                DeepLink.Kind.Wishlist => await HandleWishlistLink(userContext, user, parameters),
                DeepLink.Kind.Broadcast => await HandleBroadcastLink(userContext, user, parameters),
                DeepLink.Kind.AdminUser => await HandleAdminUserLink(userContext, user, parameters),
                DeepLink.Kind.Settings => await HandleSettingsLink(userContext, user),
                DeepLink.Kind.Profile => await HandleProfileLink(userContext, user),
                _ => false,
            };

            if (handled)
                return;
        }

        await Sender.SendOrEdit(userContext, user.UserId, new MainMenuMessage(Logger), forceNewMessage: true);
    }

    public override bool ShouldCleanup(string actionText) =>
        !TryParseInviteLink(actionText, out _) && DeepLink.TryParse(actionText, out _, out _);

    private async Task HandleInviteLink(UserContext userContext, UserModel user, string subscribeId)
    {
        var userToAddAsFriend = userContext.Users.FirstOrDefault(u => u.SubscribeId == subscribeId);

        if (userToAddAsFriend is null)
        {
            await Sender.SendOrEdit(userContext, user.UserId, new FailFriendLinkMessage(Logger), forceNewMessage: true);
            return;
        }

        if (userToAddAsFriend == user)
        {
            await Sender.SendOrEdit(userContext, user.UserId, new MainMenuMessage(Logger), forceNewMessage: true);
            return;
        }

        SetQueryParams(user, new QueryParameter(QueryParameterType.UserId, userToAddAsFriend.UserId));
        await Sender.SendOrEdit(userContext, user.UserId, new AddFriendMessage(Logger), forceNewMessage: true);
    }

    private async Task<bool> HandleWishLink(UserContext userContext, UserModel user, IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue(DeepLink.UserKey, out var userSubscribeId)
            || !parameters.TryGetValue(DeepLink.WishIdKey, out var wishIdStr)
            || !int.TryParse(wishIdStr, out var wishId))
        {
            return false;
        }

        var wishOwner = userContext.Users.FirstOrDefault(u => u.SubscribeId.StartsWith(userSubscribeId));
        if (wishOwner is null)
            return false;

        var queryParameters = new List<QueryParameter>
        {
            new(QueryParameterType.UserId, wishOwner.UserId),
            new(QueryParameterType.WishId, wishId),
        };

        // Ссылка из списка броней должна вернуть обратно в этот список
        if (parameters.TryGetValue(DeepLink.ReturnToClaimsKey, out var claimsFlag) && claimsFlag == "1")
            queryParameters.Add(QueryParameter.ReturnToMyClaims);

        SetQueryParams(user, queryParameters.ToArray());
        await Sender.SendOrEdit(userContext, user.UserId, new ShowWishMessage(Logger));
        return true;
    }

    private async Task<bool> HandleFriendLink(UserContext userContext, UserModel user, IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue(DeepLink.UserKey, out var friendSubscribeId))
            return false;

        var friend = userContext.Users.FirstOrDefault(u => u.SubscribeId.StartsWith(friendSubscribeId));
        if (friend is null)
            return false;

        SetQueryParams(user, new QueryParameter(QueryParameterType.UserId, friend.UserId));
        await Sender.SendOrEdit(userContext, user.UserId, new FriendMessage(Logger));
        return true;
    }

    private async Task<bool> HandleWishlistLink(UserContext userContext, UserModel user, IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue(DeepLink.UserKey, out var wishlistOwnerSubscribeId))
            return false;

        var wishlistOwner = userContext.Users.FirstOrDefault(u => u.SubscribeId.StartsWith(wishlistOwnerSubscribeId));
        if (wishlistOwner is null)
            return false;

        // Страницу не передаём: вишлист открывается на той, где зритель его оставил
        SetQueryParams(user,
            new QueryParameter(QueryParameterType.UserId, wishlistOwner.UserId),
            QueryParameter.ReturnToFriends);

        await Sender.SendOrEdit(userContext, user.UserId, new CompactListMessage(Logger));
        return true;
    }

    private async Task<bool> HandleBroadcastLink(UserContext userContext, UserModel user, IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue(DeepLink.BroadcastIdKey, out var broadcastIdStr)
            || !int.TryParse(broadcastIdStr, out var broadcastId))
        {
            return false;
        }

        if (!user.IsAdmin)
        {
            Logger.Warning("Broadcast link called, but [{id}] is not admin", user.UserId);
            return true;
        }

        SetQueryParams(user, new QueryParameter(QueryParameterType.SetBroadcastTo, broadcastId));
        await Sender.SendOrEdit(userContext, user.UserId, new BroadcastMessage(Logger));
        return true;
    }

    private async Task<bool> HandleAdminUserLink(UserContext userContext, UserModel user, IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue(DeepLink.UserKey, out var userIdStr)
            || !int.TryParse(userIdStr, out var userId))
        {
            return false;
        }

        if (!user.IsAdmin)
        {
            Logger.Warning("Admin user link called, but [{id}] is not admin", user.UserId);
            return true;
        }

        SetQueryParams(user, new QueryParameter(QueryParameterType.UserId, userId));
        await Sender.SendOrEdit(userContext, user.UserId, new UserMessage(Logger));
        return true;
    }

    private async Task<bool> HandleSettingsLink(UserContext userContext, UserModel user)
    {
        await Sender.SendOrEdit(userContext, user.UserId, new SettingsMessage(Logger));
        return true;
    }

    private async Task<bool> HandleProfileLink(UserContext userContext, UserModel user)
    {
        await Sender.SendOrEdit(userContext, user.UserId, new EditProfileMessage(Logger));
        return true;
    }

    private static void SetQueryParams(UserModel user, params QueryParameter[] parameters) =>
        user.QueryParams = new QueryParameterCollection(parameters).ToString();

    private static bool TryParseInviteLink(string actionText, out string subscribeId)
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
}
