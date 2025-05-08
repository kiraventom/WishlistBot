using Telegram.Bot.Types;
using WishlistBot.Model;

namespace WishlistBot.Listeners;

public interface IListener
{
   Task<bool> HandleMessageAsync(Message message, UserContext userContext, int userId);
}
