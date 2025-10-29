using Telegram.Bot.Types;
using WishlistBot.Model.User;

namespace WishlistBot.Listeners;

public interface IListener
{
   Task<HandleResult> HandleMessageAsync(Message message, UserContext userContext, int userId);
}
