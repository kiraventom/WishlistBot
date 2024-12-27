using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;
using WishlistBot.Actions;
using WishlistBot.Listeners;

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
         await HandleCallbackQueryAsync(callbackQuery);
   }

   private async Task HandleMessageAsync(Message message)
   {
      var sender = message.From!;

      logger.Information("Received '{text}' ([{messageId}]) from '{first} {last}' (@{tag} [{id}])", message.Text, message.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

      var botCommand = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
      if (botCommand is not null)
      {
         await HandleBotCommandAsync(botCommand, message.Text, user);
         return;
      }

      foreach (var listener in listeners)
      {
         if (await listener.HandleMessageAsync(message, user))
            break;
      }
   }

   private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
   {
      var sender = callbackQuery.From!;

      logger.Information("Received callback query ([{callbackQueryId}]) with data '{data}' from '{first} {last}' (@{tag} [{id}])", callbackQuery.Id, callbackQuery.Data, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

      if (callbackQuery.Data is null)
      {
         logger.Warning("Callback query [{callbackQueryId}] does not contain data", callbackQuery.Id);
         return;
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
