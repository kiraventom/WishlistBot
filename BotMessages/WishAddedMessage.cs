using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class WishAddedMessage : BotMessage
{
   public WishAddedMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard()
         .AddButton<AddWishQuery>("Добавить ещё виш")
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      Text = "Виш добавлен!";

      user.BotState = BotState.WishAdded;
   }
}
