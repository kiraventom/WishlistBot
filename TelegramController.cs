using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Database.Users;
using WishlistBot.Actions;
using WishlistBot.Listeners;
using WishlistBot.Text;
using WishlistBot.Model;

namespace WishlistBot;

public class TelegramController(ILogger logger, ITelegramBotClient client, UsersDb usersDb, IReadOnlyCollection<UserAction> actions, IReadOnlyCollection<IListener> listeners)
{
    private bool _started;

    public void StartReceiving()
    {
        if (_started)
        {
            logger.Error("Tried to start {className} twice", nameof(TelegramController));
            return;
        }

        _started = true;

        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };

        client.StartReceiving(OnUpdate, OnError, receiverOptions);
        logger.Information("Started listening");
    }

    private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        logger.Information("Received update: {updateType}", update.Type);

        const bool legacy = false;
        if (legacy)
        {
            if (update.Message is { } message)
                await Legacy_HandleMessageAsync(message);

            if (update.CallbackQuery is { } callbackQuery)
                await Legacy_HandleCallbackQueryAsync(callbackQuery, client);
        }
        else
        {
            using (var userContext = UserContext.Create())
            {
                // TODO 
                /* using (var transaction = userContext.Database.BeginTransaction()) */
                {
                    try
                    {
                        if (update.Message is { } message)
                            await HandleMessageAsync(message, userContext);

                        if (update.CallbackQuery is { } callbackQuery)
                            await HandleCallbackQueryAsync(callbackQuery, client, userContext);

                        userContext.SaveChanges();
                        /* transaction.Commit(); */
                    }
                    catch (Exception e)
                    {
                        /* transaction.Rollback(); */
                        logger.Error(e.ToString());
                    }
                }
            }
        }
    }

    private async Task HandleMessageAsync(Message message, UserContext userContext)
    {
        var sender = message.From!;

        logger.Information("Received '{text}' ([{messageId}]) from '{first} {last}' (@{tag} [{id}])", message.Text, message.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

        var user = userContext.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

        foreach (var listener in listeners)
        {
            if (await listener.HandleMessageAsync(message, userContext, user.UserId))
                return;
        }

        var botCommand = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
        if (botCommand is not null)
        {
            await HandleBotCommandAsync(userContext, user, botCommand, message.Text);
        }
    }

    private async Task Legacy_HandleMessageAsync(Message message)
    {
        var sender = message.From!;

        logger.Information("Received '{text}' ([{messageId}]) from '{first} {last}' (@{tag} [{id}])", message.Text, message.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

        var user = usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

        foreach (var listener in listeners)
        {
            if (await listener.Legacy_HandleMessageAsync(message, user))
                return;
        }

        var botCommand = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
        if (botCommand is not null)
        {
            await Legacy_HandleBotCommandAsync(botCommand, message.Text, user);
        }
    }

    private async Task Legacy_HandleCallbackQueryAsync(CallbackQuery callbackQuery, ITelegramBotClient client)
    {
        var sender = callbackQuery.From!;

        logger.Information("Received callback query ([{callbackQueryId}]) with data '{data}' on message [{messageId}] from '{first} {last}' (@{tag} [{id}])", callbackQuery.Id, callbackQuery.Data, callbackQuery.Message?.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

        var user = usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

        if (callbackQuery.Message is null || callbackQuery.Message.MessageId != user.LastBotMessageId)
        {
            await Legacy_HandleOldMessageQuery(callbackQuery, client, user);
            return;
        }

        if (callbackQuery.Data is null)
        {
            logger.Warning("Callback query [{callbackQueryId}] does not contain data", callbackQuery.Id);
            return;
        }

        if (!string.IsNullOrEmpty(user.AllowedQueries))
        {
            var allowedQueries = user.AllowedQueries.Split(';');
            if (!allowedQueries.Contains(callbackQuery.Data))
            {
                logger.Warning("Unexpected query [{queryId}] '{query}', allowed queries: '{allowed}'", callbackQuery.Id, callbackQuery.Data, user.AllowedQueries);
                return;
            }
        }

        user.LastQueryId = callbackQuery.Id;
        await Legacy_HandleUserActionAsync(callbackQuery.Data, callbackQuery.Data, user);
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, ITelegramBotClient client, UserContext userContext)
    {
        var sender = callbackQuery.From!;

        logger.Information("Received callback query ([{callbackQueryId}]) with data '{data}' on message [{messageId}] from '{first} {last}' (@{tag} [{id}])", callbackQuery.Id, callbackQuery.Data, callbackQuery.Message?.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

        var userModel = userContext.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

        if (callbackQuery.Message is null || callbackQuery.Message.MessageId < userModel.LastBotMessageId)
        {
            await HandleOldMessageQuery(callbackQuery, client, userModel);
            return;
        }

        if (callbackQuery.Data is null)
        {
            logger.Warning("Callback query [{callbackQueryId}] does not contain data", callbackQuery.Id);
            return;
        }

        if (!string.IsNullOrEmpty(userModel.AllowedQueries))
        {
            var allowedQueries = userModel.AllowedQueries.Split(';');
            if (!allowedQueries.Contains(callbackQuery.Data))
            {
                logger.Warning("Unexpected query [{queryId}] '{query}', allowed queries: '{allowed}'", callbackQuery.Id, callbackQuery.Data, userModel.AllowedQueries);
                return;
            }
        }

        userModel.LastQueryId = callbackQuery.Id;
        await HandleUserActionAsync(userContext, userModel, callbackQuery.Data, callbackQuery.Data);
    }

    private async Task HandleOldMessageQuery(CallbackQuery callbackQuery, ITelegramBotClient client, UserModel user)
    {
        logger.Warning("Query from old message [{oldMessageId}] (last message id [{lastMessageId}]. Editing to placeholder and returning", callbackQuery.Message?.MessageId, user.LastBotMessageId);

        const string text = "Управление из старых сообщений не поддерживается. \nИспользуйте последнее сообщение или отправьте команду /start";
        var messageText = new MessageText(text);
        const string placeholderImageFileId = "AgACAgIAAxkBAAII32e_pW96dETJX11gzLRzXIJhyoDQAAKk8zEbHCn5SThcc9XkHwp6AQADAgADcwADNgQ";

        var photo = new InputMediaPhoto(InputFile.FromFileId(placeholderImageFileId));

        var keyboardMarkup = new InlineKeyboardMarkup() { InlineKeyboard = Enumerable.Empty<IEnumerable<InlineKeyboardButton>>() };

        if (callbackQuery.Message is not null)
        {
            await client.EditMessageMedia(chatId: user.TelegramId, messageId: callbackQuery.Message.MessageId, media: photo, replyMarkup: keyboardMarkup);
            await client.EditMessageCaption(chatId: user.TelegramId, messageId: callbackQuery.Message.MessageId, caption: messageText.ToString(), replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
        }
        else
        {
            logger.Warning("CallbackQuery.Message is null!");
            var file = InputFile.FromFileId(placeholderImageFileId);
            var message = await client.SendPhoto(chatId: user.TelegramId, photo: file, caption: messageText.ToString(), replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
            user.LastBotMessageId = message.MessageId;
        }

        await client.AnswerCallbackQuery(callbackQuery.Id);
    }

    private async Task Legacy_HandleOldMessageQuery(CallbackQuery callbackQuery, ITelegramBotClient client, BotUser user)
    {
        logger.Warning("Query from old message [{oldMessageId}] (last message id [{lastMessageId}]. Editing to placeholder and returning", callbackQuery.Message?.MessageId, user.LastBotMessageId);

        const string text = "Управление из старых сообщений не поддерживается. \nИспользуйте последнее сообщение или отправьте команду /start";
        var messageText = new MessageText(text);
        const string placeholderImageFileId = "AgACAgIAAxkBAAII32e_pW96dETJX11gzLRzXIJhyoDQAAKk8zEbHCn5SThcc9XkHwp6AQADAgADcwADNgQ";

        var photo = new InputMediaPhoto(InputFile.FromFileId(placeholderImageFileId));

        var keyboardMarkup = new InlineKeyboardMarkup() { InlineKeyboard = Enumerable.Empty<IEnumerable<InlineKeyboardButton>>() };

        if (callbackQuery.Message is not null)
        {
            await client.EditMessageMedia(chatId: user.SenderId, messageId: callbackQuery.Message.MessageId, media: photo, replyMarkup: keyboardMarkup);
            await client.EditMessageCaption(chatId: user.SenderId, messageId: callbackQuery.Message.MessageId, caption: messageText.ToString(), replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
        }
        else
        {
            logger.Warning("CallbackQuery.Message is null!");
            var file = InputFile.FromFileId(placeholderImageFileId);
            var message = await client.SendPhoto(chatId: user.SenderId, photo: file, caption: messageText.ToString(), replyMarkup: keyboardMarkup, parseMode: ParseMode.MarkdownV2);
            user.LastBotMessageId = message.MessageId;
        }

        await client.AnswerCallbackQuery(callbackQuery.Id);
    }

    private async Task HandleBotCommandAsync(UserContext userContext, UserModel userModel, MessageEntity botCommand, string messageText)
    {
        var commandText = messageText.Substring(botCommand.Offset, botCommand.Length);
        await HandleUserActionAsync(userContext, userModel, commandText, messageText);
    }

    private async Task Legacy_HandleBotCommandAsync(MessageEntity botCommand, string messageText, BotUser user)
    {
        var commandText = messageText.Substring(botCommand.Offset, botCommand.Length);
        await Legacy_HandleUserActionAsync(commandText, messageText, user);
    }

    private async Task HandleUserActionAsync(UserContext userContext, UserModel userModel, string actionText, string fullText)
    {
        var action = actions.FirstOrDefault(c => c.IsMatch(actionText));
        if (action is null)
        {
            logger.Warning("Action '{actionText}' does not match any of actions: [ {actions} ]", actionText, string.Join(", ", actions.Select(c => c.Name)));
            return;
        }

        await action.ExecuteAsync(userContext, userModel, fullText);
    }

    private async Task Legacy_HandleUserActionAsync(string actionText, string fullText, BotUser user)
    {
        var action = actions.FirstOrDefault(c => c.IsMatch(actionText));
        if (action is null)
        {
            logger.Warning("Action '{actionText}' does not match any of actions: [ {actions} ]", actionText, string.Join(", ", actions.Select(c => c.Name)));
            return;
        }

        await action.Legacy_ExecuteAsync(user, fullText);
    }

    private static Task OnError(ITelegramBotClient client, Exception exception, CancellationToken ct) => Task.CompletedTask;
}
