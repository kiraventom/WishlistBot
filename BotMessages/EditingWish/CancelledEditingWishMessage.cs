using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class CancelledEditingWishMessage : BotMessage
{
   public CancelledEditingWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (parameters.Pop(QueryParameterType.ReturnToEditList))
      {
         Text = "Редактирование виша отменено";
         Keyboard.AddButton<EditListQuery>("Назад к редактированию");
      }
      else
      {
         Text = "Создание виша отменено";
         Keyboard.AddButton<SetWishNameQuery>("Добавить другой виш")
            .NewRow()
            .AddButton<MyWishesQuery>("Назад к моим вишам");
      }


      user.CurrentWish = null;
   }
}
