using Telegram.Bot.Types;
using WishlistBot.Database.Users;

namespace WishlistBot.Listeners;

public interface IListener
{
   Task<bool> HandleMessageAsync(Message message, BotUser user);
}
