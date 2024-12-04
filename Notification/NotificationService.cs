using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Database.MediaStorage;
using WishlistBot.Database.Users;

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
}
