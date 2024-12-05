using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

public class EditWishFailedMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<SetWishNameQuery>("Добавить другой виш")
         .NewRow()
         .AddButton<CompactListQuery>("Назад к моим вишам");

      Text.Italic("Создание виша не удалось");

      user.CurrentWish = null;
   }
}
