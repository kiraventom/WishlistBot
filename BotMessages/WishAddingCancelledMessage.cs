using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class WishAddingCancelledMessage : BotMessage
{
   public WishAddingCancelledMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard();
      Text = "Создание виша отменено";
   }
}
