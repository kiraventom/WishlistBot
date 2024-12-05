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

   public async Task Send(BotMessage notification, BotUser user)
   {
      var recipients = _usersDb.Values.Values
         .Where(u => u.Subscriptions.Contains(user.SubscribeId));
      foreach (var recipient in recipients)
         await _client.SendOrEditBotMessage(_logger, recipient, notification, forceNewMessage: true);
   }
}
