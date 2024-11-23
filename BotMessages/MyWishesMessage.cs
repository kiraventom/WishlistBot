using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class MyWishesMessage : BotMessage
{
   public MyWishesMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>("Добавить виш", QueryParameter.ForceNewWish)
         .NewRow()
         .AddButton<CompactListMyWishesQuery>("Список вишей")
//       .AddButton<FullListMyWishesQuery>()
         .NewRow()
         .AddButton<MainMenuQuery>("Назад");

      Text = $"Ваши виши\n\nКоличество вишей в вашем списке: {user.Wishes.Count}";

      user.BotState = BotState.MyWishes;
   }
}
