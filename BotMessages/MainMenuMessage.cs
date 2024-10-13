using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class MainMenuMessage : BotMessage
{
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
