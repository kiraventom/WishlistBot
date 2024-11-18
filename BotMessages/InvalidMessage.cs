using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class InvalidMessage : BotMessage
{
   public InvalidMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user)
   {
      Keyboard = new BotKeyboard();

      Text = "Некорректное действие";
   }
}
