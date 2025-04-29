using Telegram.Bot;
using Serilog;
using WishlistBot.Model;

namespace WishlistBot.Actions;

public abstract class UserAction(ILogger logger, ITelegramBotClient client)
{
   protected ILogger Logger { get; } = logger;
   protected ITelegramBotClient Client { get; } = client;

   public abstract string Name { get; }

   public abstract Task ExecuteAsync(UserContext userContext, UserModel userModel, string actionText);

   public virtual bool IsMatch(string name) => name == Name;
}
