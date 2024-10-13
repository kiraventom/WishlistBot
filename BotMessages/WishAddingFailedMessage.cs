using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class WishAddingFailedMessage : BotMessage
{
   public WishAddingFailedMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard();
      Text = "Ни одного сообщения не было отправлено, создание виша отменено";
   }
}
