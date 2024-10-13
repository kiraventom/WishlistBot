using Telegram.Bot;
using Serilog;
using WishlistBot.Database;

namespace WishlistBot.Actions;

public abstract class UserAction
{
   protected ILogger Logger { get; }
   protected ITelegramBotClient Client { get; }

   public abstract string Name { get; }

   protected UserAction(ILogger logger, ITelegramBotClient client)
   {
      Logger = logger;
      Client = client;
   }

   public abstract Task ExecuteAsync(BotUser user);

   public bool IsMatch(string name)
   {
      return name == Name;
   }
}
