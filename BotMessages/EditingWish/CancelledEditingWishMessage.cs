using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class CancelledEditingWishMessage : BotMessage
{
   public CancelledEditingWishMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard()
         .AddButton<SetWishNameQuery>("Добавить другой виш")
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      Text = "Создание виша отменено";

      user.CurrentWish = null;
   }
}
