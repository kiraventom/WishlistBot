using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class InvalidMessage : BotMessage
{
   public InvalidMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard();

      Text = "Некорректное действие";
   }
}
