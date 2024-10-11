using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using WishlistBot.Users;

namespace WishlistBot;

public class TelegramController
{
   private readonly ILogger _logger;
   private readonly TelegramBotClient _client;
   private readonly UsersDb _usersDb;

   private readonly IReadOnlyCollection<Command> _commands;

   private bool _started;

   public TelegramController(ILogger logger, string token, UsersDb usersDb)
   {
      _logger = logger;
      _client = new TelegramBotClient(token);
      _usersDb = usersDb;

      _commands = new Command[]
      {
      };
   }

   public void StartReceiving()
   {
      if (_started)
      {
         _logger.Error("Tried to start {className} twice", nameof(TelegramController));
         return;
      }

      _started = true;

      var receiverOptions = new ReceiverOptions()
      {
         AllowedUpdates = new[] {UpdateType.Message}
      };

      _client.StartReceiving(OnUpdate, OnError, receiverOptions);
      _logger.Information("Started listening");
   }

   private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
   {
      _logger.Information("Received update: {updateType}", update.Type);

      if (update.Message is Message message)
         await HandleMessageAsync(client, message);
   }

   private async Task HandleMessageAsync(ITelegramBotClient client, Message message)
   {
      User sender = message.From;
      if (message.Text is null)
         return;

      _logger.Information("Received '{text}' ([{messageId}]) from '{first} {last}' (@{tag} [{id}])", message.Text, message.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = _usersDb.GetOrAddUser(sender.Id);

      if (message.Entities is null)
         return;
      
      var botCommand = message.Entities.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
      if (botCommand is not null)
      {
         await HandleBotCommandAsync(botCommand, message.Text, user);
         return;
      }
   }

   private async Task HandleBotCommandAsync(MessageEntity botCommand, string messageText, BotUser user)
   {
      var commandText = messageText.Substring(botCommand.Offset, botCommand.Length);
      var command = _commands.FirstOrDefault(c => c.IsMatch(commandText));
      if (command is null)
      {
         _logger.Warning("Command '{commandText}' does not match any of commands: [ {commands} ]", commandText, string.Join(", ", _commands.Select(c => c.Name)));
         return;
      }

      await command.ExecuteAsync(user);
   }

   private Task OnError(ITelegramBotClient client, Exception exception, CancellationToken cts)
   {
      _logger.Error(exception.ToString());
      return Task.CompletedTask;
   }
}
