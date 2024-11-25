using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class EditingWishFailedMessage : BotMessage
{
   public EditingWishFailedMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>("Добавить другой виш")
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      Text = "Создание виша не удалось";

      user.CurrentWish = null;
   }
}
