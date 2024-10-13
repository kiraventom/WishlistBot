using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Users;

namespace WishlistBot.BotMessages;

public class MainMenuMessage : BotMessage
{
   public override string Text { get; }

   public override BotKeyboard Keyboard { get; }

   public MainMenuMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard()
         .AddButton<MyWishesQuery>()
         .AddButton("@my_subscriptions", "Мои подписки")
         .AddButton("@settings", "Настройки");

      Text = $"Добро пожаловать в главное меню, {user.FirstName}!";

      user.BotState = BotState.MainMenu;
   }
}
