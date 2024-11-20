using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class CancelledEditingWishMessage : BotMessage
{
   public CancelledEditingWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>("Добавить другой виш")
         .NewRow();

      if (parameters.Pop(QueryParameterType.ReturnToEditList))
         Keyboard.AddButton<EditListQuery>("Назад к редактированию"); // TODO pass page as parameter here
      else
         Keyboard.AddButton<MyWishesQuery>("Назад к моим вишам");

      Text = "Создание виша отменено";

      user.CurrentWish = null;
   }
}
