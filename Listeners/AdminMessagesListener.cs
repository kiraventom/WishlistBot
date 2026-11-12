using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.BotMessages.Admin.Broadcasts;
using WishlistBot.Media;
using WishlistBot.Model;
using WishlistBot.Model.User;

namespace WishlistBot.Listeners;

public class AdminMessagesListener(ILogger logger, BotMessageSender sender) : IListener
{
    public async Task<HandleResult> HandleMessageAsync(Message message, UserContext userContext, int userId)
    {
        var user = userContext.Users.First(u => u.UserId == userId);
        switch (user.BotState)
        {
            case BotState.ListenForBroadcast:
                await HandleBroadcastAsync(userContext, message, user);
                break;

            default:
                return false;
        }

        return true;
    }

    private async Task HandleBroadcastAsync(UserContext userContext, Message message, UserModel user)
    {
        var text = message.Text ?? message.Caption;
        var photo = message.Photo?.LastOrDefault();

        if (string.IsNullOrEmpty(text))
        {
            logger.Warning("Received no text, ignoring");
            return;
        }

        if (message.MediaGroupId is not null)
            logger.Warning("Media groups are not supported, ignoring other photos");

        var broadcast = new BroadcastModel()
        {
            Text = text,
            MediaKey = photo is null ? null : await MediaStorage.Instance.RegisterAsync(photo)
        };

        await SendFinishAddBroadcastMessage(userContext, user, broadcast);
    }

    private async Task SendFinishAddBroadcastMessage(UserContext userContext, UserModel user, BroadcastModel broadcast)
    {
        var message = new FinishAddBroadcastMessage(logger, broadcast);
        await sender.SendOrEdit(userContext, user.UserId, message, forceNewMessage: true);
    }
}
