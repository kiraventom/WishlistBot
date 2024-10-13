using Serilog;
using WishlistBot.Keyboard;

namespace WishlistBot.BotMessages;

public abstract class BotMessage
{
   protected ILogger Logger { get; }

   public abstract string Text { get; }
   public abstract BotKeyboard Keyboard { get; }

   protected BotMessage(ILogger logger)
   {
      Logger = logger;
   }
}
