using Serilog;
using WishlistBot.Model.User;

namespace WishlistBot.Actions;

public abstract class UserAction(ILogger logger, BotMessageSender sender)
{
   protected ILogger Logger { get; } = logger;
   protected BotMessageSender Sender { get; } = sender;

   public abstract string Name { get; }

   public abstract Task ExecuteAsync(UserContext userContext, UserModel userModel, string actionText);

   public virtual bool IsMatch(string name) => name == Name;
   public virtual bool ShouldCleanup(string actionText) => false;
}
