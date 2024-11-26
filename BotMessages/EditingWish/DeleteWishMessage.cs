using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditingWish;

public class DeleteWishMessage : BotMessage
{
   public DeleteWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<MyWishesQuery>("Назад к моим вишам");

      user.BotState = BotState.WishDeleted;
      
      Text.Italic("Виш удалён!");

      user.Wishes.Remove(user.CurrentWish);
      user.CurrentWish = null;
   }
}
