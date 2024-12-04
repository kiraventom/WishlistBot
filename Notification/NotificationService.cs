using Serilog;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages;

namespace WishlistBot.Notification;

public class NotificationService
{
   private bool _inited;

   private ILogger _logger;
   private ITelegramBotClient _client;
   private UsersDb _usersDb;

   public static NotificationService Instance { get; } = new NotificationService();

   public void Init(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      if (_inited)
         return;

      _logger = logger;

      _client = client;
      _usersDb = usersDb;
      _inited = true;
   }

   public async Task Send(BotMessage notification, IEnumerable<BotUser> recepients)
   {
      foreach (var recepient in recepients)
      {
         await _client.SendOrEditBotMessage(_logger, recepient, notification, forceNewMessage: true);
      }
   }
}
