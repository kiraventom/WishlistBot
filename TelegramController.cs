using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using WishlistBot.Database;
using WishlistBot.Actions;
using WishlistBot.Actions.Commands;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.BotMessages;

namespace WishlistBot;

public class TelegramController
{
   private readonly ILogger _logger;
   private readonly TelegramBotClient _client;
   private readonly UsersDb _usersDb;
   private readonly WishMessagesListener _wishMessagesListener;

   private readonly IReadOnlyCollection<UserAction> _actions;

   private bool _started;

   public TelegramController(ILogger logger, string token, UsersDb usersDb)
   {
      _logger = logger;
      _client = new TelegramBotClient(token);
      _usersDb = usersDb;
      _wishMessagesListener = new WishMessagesListener(_logger, _client);

      var messagesFactory = new MessageFactory(_logger, _client);

      _actions = new UserAction[]
      {
         new StartCommand(_logger, _client),
         new QueryAction<MainMenuQuery>(_logger, _client, messagesFactory),
         new QueryAction<MyWishesQuery>(_logger, _client, messagesFactory),
         new QueryAction<CompactListMyWishesQuery>(_logger, _client, messagesFactory),
         new QueryAction<FullListMyWishesQuery>(_logger, _client, messagesFactory),
         new QueryAction<EditWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<DeleteWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<ConfirmWishDeletionQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishNameQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishDescriptionQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishMediaQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishLinksQuery>(_logger, _client, messagesFactory),
         new QueryAction<CancelEditingWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<FinishEditingWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<EditListQuery>(_logger, _client, messagesFactory),
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
         AllowedUpdates = new[] {UpdateType.Message, UpdateType.CallbackQuery}
      };

      _client.StartReceiving(OnUpdate, OnError, receiverOptions);
      _logger.Information("Started listening");
   }

   private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
   {
      _logger.Information("Received update: {updateType}", update.Type);

      if (update.Message is Message message)
         await HandleMessageAsync(client, message);

      if (update.CallbackQuery is CallbackQuery callbackQuery)
         await HandleCallbackQueryAsync(client, callbackQuery);
   }

   private async Task HandleMessageAsync(ITelegramBotClient client, Message message)
   {
      User sender = message.From;

      _logger.Information("Received '{text}' ([{messageId}]) from '{first} {last}' (@{tag} [{id}])", message.Text, message.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = _usersDb.GetOrAddUser(sender.Id, sender.FirstName);

      var botCommand = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
      if (botCommand is not null)
      {
         await HandleBotCommandAsync(botCommand, message.Text, user);
         return;
      }

      await _wishMessagesListener.HandleWishMessageAsync(message, user);
   }

   private async Task HandleCallbackQueryAsync(ITelegramBotClient client, CallbackQuery callbackQuery)
   {
      User sender = callbackQuery.From;

      _logger.Information("Received callback query ([{callbackQueryId}]) with data '{data}' from '{first} {last}' (@{tag} [{id}])", callbackQuery.Id, callbackQuery.Data, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = _usersDb.GetOrAddUser(sender.Id, sender.FirstName);

      if (callbackQuery.Data is null)
      {
         _logger.Warning("Callback query [{callbackQueryId}] does not contain data", callbackQuery.Id);
         return;
      }
      
      user.LastQueryId = callbackQuery.Id;
      await HandleUserActionAsync(callbackQuery.Data, user);
   }

   private async Task HandleBotCommandAsync(MessageEntity botCommand, string messageText, BotUser user)
   {
      var commandText = messageText.Substring(botCommand.Offset, botCommand.Length);
      await HandleUserActionAsync(commandText, user);
   }

   private async Task HandleUserActionAsync(string actionText, BotUser user)
   {
      var action = _actions.FirstOrDefault(c => c.IsMatch(actionText));
      if (action is null)
      {
         _logger.Warning("Action '{actionText}' does not match any of actions: [ {actions} ]", actionText, string.Join(", ", _actions.Select(c => c.Name)));
         return;
      }

      await action.ExecuteAsync(user, actionText);
   }

   private Task OnError(ITelegramBotClient client, Exception exception, CancellationToken cts)
   {
      return Task.CompletedTask;
   }
}
