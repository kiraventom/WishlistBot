using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Users;

namespace WishlistBot.BotMessages;

public class MyWishesMessage : BotMessage
{
   public override string Text { get; }

   public override BotKeyboard Keyboard { get; }

   public MyWishesMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard()
         .AddButton("@list", "Список")
         .AddButton<MainMenuQuery>("Назад");

      Text = "Ваши желания";

      user.BotState = BotState.MyWishes;
   }
}
