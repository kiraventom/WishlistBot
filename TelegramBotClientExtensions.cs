using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.BotMessages;
using WishlistBot.Model.User;

namespace WishlistBot;

/// <summary>
/// Совместимость: отправка экранов переехала в <see cref="BotMessageSender"/>.
/// Оставлено для харнесов в <c>/home/user/work/wishlistbot</c>, которые открывают
/// экраны через поддельный <see cref="ITelegramBotClient"/>. В самом боте не использовать.
/// </summary>
[Obsolete("Используйте BotMessageSender: SendOrEdit / ClearKeyboard. Оставлено только для тестовых харнесов.")]
public static class TelegramBotClientExtensions
{
    public static Task<Message> SendOrEditBotMessage(this ITelegramBotClient client, ILogger logger, UserContext userContext, int userId, BotMessage botMessage, bool forceNewMessage = false) =>
        new BotMessageSender(logger, client).SendOrEdit(userContext, userId, botMessage, forceNewMessage);

    public static Task ClearKeyboard(this ITelegramBotClient client, ILogger logger, long userId, long telegramMessageId, bool suppressLog = false) =>
        new BotMessageSender(logger, client).ClearKeyboard(userId, telegramMessageId, suppressLog);
}
