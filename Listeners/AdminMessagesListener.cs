using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.BotMessages.Admin.Broadcasts;
using WishlistBot.Model;

namespace WishlistBot.Listeners;

public class AdminMessagesListener(ILogger logger, ITelegramBotClient client) : IListener
{
    public async Task<bool> HandleMessageAsync(Message message, UserContext userContext, int userId)
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
        var fileId = message.Photo?.FirstOrDefault()?.FileId;

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
            FileId = fileId
        };

        await SendFinishAddBroadcastMessage(userContext, user, broadcast);
    }

    private async Task SendFinishAddBroadcastMessage(UserContext userContext, UserModel user, BroadcastModel broadcast)
    {
        var message = new FinishAddBroadcastMessage(logger, broadcast);
        await client.SendOrEditBotMessage(logger, userContext, user.UserId, message, forceNewMessage: true);
    }
}
