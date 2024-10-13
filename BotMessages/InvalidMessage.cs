using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Users;

namespace WishlistBot.BotMessages;

public class InvalidMessage : BotMessage
{
   public override string Text { get; }

   public override BotKeyboard Keyboard { get; }

   public InvalidMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard();

      Text = $"ОШИБКА: действие не поддерживается";

      logger.Error("Failed to find message for query [{queryId}]", user.LastQueryId);
   }
}
