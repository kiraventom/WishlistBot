using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class CancelledEditingWishMessage : BotMessage
{
   public CancelledEditingWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, params QueryParameter[] parameters)
   {
      Keyboard = new BotKeyboard()
         .AddButton<SetWishNameQuery>("Добавить другой виш")
         .NewRow();

      if (HasParameter(parameters, QueryParameterType.ReturnToEditList))
         Keyboard.AddButton<EditListQuery>("Назад к редактированию"); // TODO pass page as parameter here
      else
         Keyboard.AddButton<MyWishesQuery>("Назад к моим вишам");

      Text = "Создание виша отменено";

      user.CurrentWish = null;
   }
}
