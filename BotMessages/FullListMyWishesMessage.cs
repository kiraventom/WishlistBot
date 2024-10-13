using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class FullListMyWishesMessage : BotMessage
{
   public FullListMyWishesMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard()
         .AddButton("@edit", "Редактировать список")
         .NewRow()
         .AddButton<CompactListMyWishesQuery>()
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      Text = "Полный список ваших вишей:";
      // TODO Send all wishes

      user.BotState = BotState.FullListMyWishes;
   }
}
