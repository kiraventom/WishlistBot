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

   public static NotificationService Instance { get; } = new();

   public void Init(ILogger logger, ITelegramBotClient client)
   {
      if (_inited)
         return;

      _logger = logger;

      _client = client;
      _inited = true;
   }

   public async Task Send(BotMessage notification, IEnumerable<BotUser> recipients)
   {
      foreach (var recipient in recipients)
         await _client.SendOrEditBotMessage(_logger, recipient, notification, forceNewMessage: true);
   }
}
