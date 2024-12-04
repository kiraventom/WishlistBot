using Telegram.Bot;
using Serilog;
using WishlistBot.Database.Users;

namespace WishlistBot.Actions;

public abstract class UserAction(ILogger logger, ITelegramBotClient client)
{
   protected ILogger Logger { get; } = logger;
   protected ITelegramBotClient Client { get; } = client;

   public abstract string Name { get; }

   public abstract Task ExecuteAsync(BotUser user, string actionText);

   public virtual bool IsMatch(string name) => name == Name;
}
