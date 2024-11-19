using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class MyWishesMessage : BotMessage
{
   public MyWishesMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, params QueryParameter[] parameters)
   {
      Keyboard = new BotKeyboard()
         .AddButton<SetWishNameQuery>("Добавить виш", QueryParameter.ForceNewWish)
         .NewRow()
         .AddButton<CompactListMyWishesQuery>()
         .AddButton<FullListMyWishesQuery>()
         .NewRow()
         .AddButton<MainMenuQuery>("Назад");

      Text = $"Ваши виши\n\nКоличество вишей в вашем списке: {user.Wishes.Count}";

      user.BotState = BotState.MyWishes;
   }
}
