using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;

namespace WishlistBot.Notification;

public class NotificationService
{
   private bool _inited;

   private ILogger _logger;
   private ITelegramBotClient _client;
   private UsersDb _usersDb;

   public static NotificationService Instance { get; } = new();

   public void Init(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      if (_inited)
         return;

      _logger = logger;
      _client = client;
      _usersDb = usersDb;

      _inited = true;
   }

   public async Task SendToSubscribers(BotMessage notification, BotUser notificationSource)
   {
      var recipients = _usersDb.Values.Values
         .Where(u => u.Subscriptions.Contains(notificationSource.SubscribeId));

      foreach (var recipient in recipients)
         await _client.SendOrEditBotMessage(_logger, recipient, notification, forceNewMessage: true);
   }

   public async Task SendToUser(BotMessage notification, BotUser notificationRecepient) =>
      await _client.SendOrEditBotMessage(_logger, notificationRecepient, notification, forceNewMessage: true);

   public async Task<int> BroadcastToUser(BotMessage notification, BotUser notificationRecepient)
   {
      var message = await _client.SendOrEditBotMessage(_logger, notificationRecepient, notification, forceNewMessage: true);
      return message.MessageId;
   }
}
