using Serilog;
using WishlistBot.Keyboard;

namespace WishlistBot.BotMessages;

public abstract class BotMessage
{
   protected ILogger Logger { get; }

   public string Text { get; protected set; }
   public BotKeyboard Keyboard { get; protected set; }
   public string PhotoFileId { get; protected set; }

   protected BotMessage(ILogger logger)
   {
      Logger = logger;
   }
}
