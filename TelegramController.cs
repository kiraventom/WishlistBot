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

      if (update.Message is { } message)
         await HandleMessageAsync(message);

      if (update.CallbackQuery is { } callbackQuery)
         await HandleCallbackQueryAsync(callbackQuery, client);
   }

   private async Task HandleMessageAsync(Message message)
   {
      var sender = message.From!;

      logger.Information("Received '{text}' ([{messageId}]) from '{first} {last}' (@{tag} [{id}])", message.Text, message.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

      foreach (var listener in listeners)
      {
         if (await listener.HandleMessageAsync(message, user))
            return;
      }

      var botCommand = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
      if (botCommand is not null)
      {
         await HandleBotCommandAsync(botCommand, message.Text, user);
      }
   }

   private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, ITelegramBotClient client)
   {
      var sender = callbackQuery.From!;

      logger.Information("Received callback query ([{callbackQueryId}]) with data '{data}' on message [{messageId}] from '{first} {last}' (@{tag} [{id}])", callbackQuery.Id, callbackQuery.Data, callbackQuery.Message?.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

      if (callbackQuery.Message is null || callbackQuery.Message.MessageId != user.LastBotMessageId)
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
      await HandleUserActionAsync(callbackQuery.Data, callbackQuery.Data, user);
   }

   private async Task HandleBotCommandAsync(MessageEntity botCommand, string messageText, BotUser user)
   {
      var commandText = messageText.Substring(botCommand.Offset, botCommand.Length);
      await HandleUserActionAsync(commandText, messageText, user);
   }

   private async Task HandleUserActionAsync(string actionText, string fullText, BotUser user)
   {
      var action = actions.FirstOrDefault(c => c.IsMatch(actionText));
      if (action is null)
      {
         logger.Warning("Action '{actionText}' does not match any of actions: [ {actions} ]", actionText, string.Join(", ", actions.Select(c => c.Name)));
         return;
      }

      await action.ExecuteAsync(user, fullText);
   }

   private static Task OnError(ITelegramBotClient client, Exception exception, CancellationToken ct) => Task.CompletedTask;
}
