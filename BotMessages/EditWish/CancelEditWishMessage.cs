using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class CancelEditWishMessage : BotMessage
{
   public CancelEditWishMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
      {
         Text.Italic("Редактирование виша отменено");
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      }
      else
      {
         Text.Italic("Создание виша отменено");
         Keyboard.AddButton<SetWishNameQuery>("Добавить другой виш")
            .NewRow()
            .AddButton<MyWishesQuery>("Назад к моим вишам");
      }

      user.CurrentWish = null;
   }
}
