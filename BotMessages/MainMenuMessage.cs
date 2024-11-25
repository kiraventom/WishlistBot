using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class MainMenuMessage : BotMessage
{
   public MainMenuMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<MyWishesQuery>()
         .AddButton("@my_subscriptions", "Мои подписки")
         .AddButton("@settings", "Настройки");

      Text.Bold("Добро пожаловать в главное меню, ")
         .InlineMention(user.FirstName, user.SenderId)
         .Bold("!");

      user.BotState = BotState.MainMenu;
   }
}
