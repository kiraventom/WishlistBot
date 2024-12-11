using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using WishlistBot.Database.Users;
using WishlistBot.Actions;
using WishlistBot.Actions.Commands;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.BotMessages;

namespace WishlistBot;

public class TelegramController
{
   private readonly ILogger _logger;
   private readonly ITelegramBotClient _client;
   private readonly UsersDb _usersDb;
   private readonly WishMessagesListener _wishMessagesListener;

   private readonly IReadOnlyCollection<UserAction> _actions;

   private bool _started;

   public TelegramController(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      _logger = logger;
      _usersDb = usersDb;
      _client = client;
      _wishMessagesListener = new WishMessagesListener(_logger, _client, _usersDb);

      var messagesFactory = new MessageFactory(_logger, _usersDb);

      _actions =
      [
         new StartCommand(_logger, _client, _usersDb),
         new QueryAction<MainMenuQuery>(_logger, _client, messagesFactory),
         new QueryAction<CompactListQuery>(_logger, _client, messagesFactory),
         new QueryAction<EditWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<DeleteWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<ConfirmDeleteWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishNameQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishDescriptionQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishMediaQuery>(_logger, _client, messagesFactory),
         new QueryAction<SetWishLinksQuery>(_logger, _client, messagesFactory),
         new QueryAction<CancelEditWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<FinishEditWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<FullListQuery>(_logger, _client, messagesFactory),
         new QueryAction<ShowWishQuery>(_logger, _client, messagesFactory),
         new QueryAction<MySubscriptionsQuery>(_logger, _client, messagesFactory),
         new QueryAction<MySubscribersQuery>(_logger, _client, messagesFactory),
         new QueryAction<ConfirmUnsubscribeQuery>(_logger, _client, messagesFactory),
         new QueryAction<UnsubscribeQuery>(_logger, _client, messagesFactory),
         new QueryAction<FinishSubscriptionQuery>(_logger, _client, messagesFactory),
         new QueryAction<SubscriberQuery>(_logger, _client, messagesFactory),
         new QueryAction<ConfirmDeleteSubscriberQuery>(_logger, _client, messagesFactory),
         new QueryAction<DeleteSubscriberQuery>(_logger, _client, messagesFactory),
         new QueryAction<SubscriptionQuery>(_logger, _client, messagesFactory)
      ];
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
         AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
      };

      _client.StartReceiving(OnUpdate, OnError, receiverOptions);
      _logger.Information("Started listening");
   }

   private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
   {
      _logger.Information("Received update: {updateType}", update.Type);

      if (update.Message is { } message)
         await HandleMessageAsync(message);

      if (update.CallbackQuery is { } callbackQuery)
         await HandleCallbackQueryAsync(callbackQuery);
   }

   private async Task HandleMessageAsync(Message message)
   {
      var sender = message.From!;

      _logger.Information("Received '{text}' ([{messageId}]) from '{first} {last}' (@{tag} [{id}])", message.Text, message.MessageId, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = _usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

      var botCommand = message.Entities?.FirstOrDefault(e => e.Type == MessageEntityType.BotCommand);
      if (botCommand is not null)
      {
         await HandleBotCommandAsync(botCommand, message.Text, user);
         return;
      }

      await _wishMessagesListener.HandleWishMessageAsync(message, user);
   }

   private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
   {
      var sender = callbackQuery.From!;

      _logger.Information("Received callback query ([{callbackQueryId}]) with data '{data}' from '{first} {last}' (@{tag} [{id}])", callbackQuery.Id, callbackQuery.Data, sender.FirstName, sender.LastName, sender.Username, sender.Id);

      var user = _usersDb.GetOrAddUser(sender.Id, sender.FirstName, sender.Username);

      if (callbackQuery.Data is null)
      {
         _logger.Warning("Callback query [{callbackQueryId}] does not contain data", callbackQuery.Id);
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
      var action = _actions.FirstOrDefault(c => c.IsMatch(actionText));
      if (action is null)
      {
         _logger.Warning("Action '{actionText}' does not match any of actions: [ {actions} ]", actionText, string.Join(", ", _actions.Select(c => c.Name)));
         return;
      }

      await action.ExecuteAsync(user, fullText);
   }

   private static Task OnError(ITelegramBotClient client, Exception exception, CancellationToken ct) => Task.CompletedTask;
}
