using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Admin;
using WishlistBot.BotMessages.Admin.Broadcasts;

namespace WishlistBot.Listeners;

public interface IListener
{
   Task<bool> HandleMessageAsync(Message message, BotUser user);
}
