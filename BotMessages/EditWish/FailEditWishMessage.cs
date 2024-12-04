using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

public class EditWishFailedMessage : BotMessage
{
   public EditWishFailedMessage(ILogger logger) : base(logger)
   {
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>("Добавить другой виш")
         .NewRow()
         .AddButton<CompactListQuery>("Назад к моим вишам");

      Text.Italic("Создание виша не удалось");

      user.CurrentWish = null;
   }
}
