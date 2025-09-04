using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Actions;
using WishlistBot.Listeners;
using WishlistBot.Text;
using WishlistBot.Model;

namespace WishlistBot;

public class TelegramController(ILogger logger, ITelegramBotClient client, IReadOnlyCollection<UserAction> actions, IReadOnlyCollection<IListener> listeners)
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

        using (var userContext = UserContext.Create())
        {
            using (var transaction = userContext.Database.BeginTransaction())
            {
                try
                {
                    if (update.Message is { } message )
                    {
                        var result = await HandleMessageAsync(message, userContext);
                        // TODO Delete message before acting
                        if (result.Cleanup)
                            await client.DeleteMessage(message.Chat.Id, message.MessageId);
                    }

                    if (update.CallbackQuery is { } callbackQuery)
                        await HandleCallbackQueryAsync(callbackQuery, client, userContext);

                    userContext.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    logger.Error(e.ToString());
                }
            }
        }
    }

    private async Task<HandleResult> HandleMessageAsync(Message message, UserContext userContext)
    {
        var sender = message.From!;

        var isNewUser = userContext.Users.All(u => u.TelegramId != sender.Id);
        var user = userContext.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

        logger.Information("Received message [{messageId}] with text '{text}' from user [{userId}] '{firstname}'{newUser}", message.MessageId, message.Text, user.UserId, user.FirstName, isNewUser ? " (new user)" : string.Empty);

        // First handle commands, so /start will always work even if listener is active
        var botCommand = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
        if (botCommand is not null)
        {
            var result = await HandleBotCommandAsync(userContext, user, botCommand, message.Text);
            if (result.Success)
                return result;
        }

        // If message did not contain command, send it to listeners
        foreach (var listener in listeners)
        {
            var result = await listener.HandleMessageAsync(message, userContext, user.UserId);
            if (result.Success)
                return result;
        }

        return false;
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, ITelegramBotClient client, UserContext userContext)
    {
        var sender = callbackQuery.From!;

        var isNewUser = userContext.Users.All(u => u.TelegramId != sender.Id);
        var user = userContext.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

        logger.Information("Received callback query [{callbackQueryId}] with data '{data}' on message [{messageId}] from user [{userId}] '{firstname}'{newUser}", callbackQuery.Id, callbackQuery.Data, callbackQuery.Message?.MessageId, user.UserId, user.FirstName, isNewUser ? " (new user)" : string.Empty);

        if (callbackQuery.Message is null || callbackQuery.Message.MessageId < user.LastBotMessageId)
        {
            await client.AnswerCallbackQuery(callbackQuery.Id, "Управление из старых сообщений не\u00a0поддерживается.\nИспользуйте последнее сообщение или\u00a0отправьте\u00a0/start", showAlert: true);
            logger.Warning("Attempt to use old message [{oldMessageId}] (last is [{lastMessageId}]. Showed alert to user [{userId}]", callbackQuery.Message.MessageId, user.LastBotMessageId, user.UserId);
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
                await client.AnswerCallbackQuery(callbackQuery.Id, "Произошла ошибка.\nИспользуйте\u00a0команду\u00a0/start и напишите разработчику\n(контакт в профиле бота)", showAlert: true);
                return;
            }
        }

        user.LastQueryId = callbackQuery.Id;
        await HandleUserActionAsync(userContext, user, callbackQuery.Data, callbackQuery.Data);
    }

    private async Task<HandleResult> HandleBotCommandAsync(UserContext userContext, UserModel userModel, MessageEntity botCommand, string messageText)
    {
        var commandText = messageText.Substring(botCommand.Offset, botCommand.Length);
        return await HandleUserActionAsync(userContext, userModel, commandText, messageText);
    }

    private async Task<HandleResult> HandleUserActionAsync(UserContext userContext, UserModel userModel, string actionText, string fullText)
    {
        var action = actions.FirstOrDefault(c => c.IsMatch(actionText));
        if (action is null)
        {
            logger.Warning("Action '{actionText}' does not match any of actions: [ {actions} ]", actionText, string.Join(", ", actions.Select(c => c.Name)));
            return false;
        }

        await action.ExecuteAsync(userContext, userModel, fullText);
        return new HandleResult { Success = true, Cleanup = action.ShouldCleanup(fullText) };
    }

    private static Task OnError(ITelegramBotClient client, Exception exception, CancellationToken ct) => Task.CompletedTask;
}
