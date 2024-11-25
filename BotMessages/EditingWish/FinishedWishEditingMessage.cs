using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class FinishedWishEditingMessage : BotMessage
{
   public FinishedWishEditingMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow();

      if (parameters.Pop(QueryParameterType.ReturnToEditList))
         Keyboard.AddButton<EditListQuery>("Назад к редактированию");
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
