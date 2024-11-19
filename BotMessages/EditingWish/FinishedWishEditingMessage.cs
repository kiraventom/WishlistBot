using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class FinishedWishEditingMessage : BotMessage
{
   public FinishedWishEditingMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, params QueryParameter[] parameters)
   {
      Keyboard = new BotKeyboard()
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow();

      if (HasParameter(parameters, QueryParameterType.ReturnToEditList))
         Keyboard.AddButton<EditListQuery>("Назад к редактированию"); // TODO pass page as parameter here
      else
         Keyboard.AddButton<MyWishesQuery>("Назад к моим вишам");

      user.BotState = BotState.WishAdded;
      
      if (!user.Wishes.Contains(user.CurrentWish))
      {
         user.Wishes.Add(user.CurrentWish);
         Text = "Виш добавлен!";
      }
      else
      {
         Text = "Виш изменён!";
      }

      user.CurrentWish = null;
   }
}
