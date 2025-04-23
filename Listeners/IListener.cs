using Telegram.Bot.Types;
using WishlistBot.Database.Users;
using WishlistBot.Model;

namespace WishlistBot.Listeners;

public interface IListener
{
   Task<bool> HandleMessageAsync(Message message, UserContext userContext, int userId);
   Task<bool> Legacy_HandleMessageAsync(Message message, BotUser user);
}
