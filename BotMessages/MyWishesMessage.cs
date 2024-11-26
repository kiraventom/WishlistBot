using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

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
         .AddButton<CompactListQuery>()
         .NewRow()
         .AddButton<MainMenuQuery>("Назад");

      Text.Bold($"Количество вишей в вашем списке: {user.Wishes.Count}");

      user.BotState = BotState.MyWishes;
   }
}
