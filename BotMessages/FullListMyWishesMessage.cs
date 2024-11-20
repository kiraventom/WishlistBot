using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class FullListMyWishesMessage : BotMessage
{
   public FullListMyWishesMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<EditListQuery>(QueryParameter.ReturnToFullList)
         .NewRow()
         .AddButton<CompactListMyWishesQuery>()
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      parameters.Pop(QueryParameterType.ReturnToFullList);

      Text = "Полный список ваших вишей:";
      // TODO Send all wishes

      user.BotState = BotState.FullListMyWishes;
   }
}
