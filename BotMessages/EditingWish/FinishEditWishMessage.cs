using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditingWish;

public class FinishEditWishMessage : BotMessage
{
   public FinishEditWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow();

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<MyWishesQuery>("Назад к моим вишам");

      user.BotState = BotState.WishAdded;
      
      if (!user.Wishes.Contains(user.CurrentWish))
      {
         user.Wishes.Add(user.CurrentWish);
         Text.Italic("Виш добавлен!");
      }
      else
      {
         Text.Italic("Виш изменён!");
      }

      user.CurrentWish = null;
   }
}
