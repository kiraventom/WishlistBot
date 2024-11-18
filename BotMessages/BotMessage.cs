using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public abstract class BotMessage
{
   private bool _isInited;

   protected ILogger Logger { get; }

   public string Text { get; protected set; }
   public BotKeyboard Keyboard { get; protected set; }
   public string PhotoFileId { get; protected set; }

   protected BotMessage(ILogger logger)
   {
      Logger = logger;
   }

   public void Init(BotUser user)
   {
      if (_isInited)
         return;

      InitInternal(user);
      _isInited = true;
   }

   protected abstract void InitInternal(BotUser user);
}
