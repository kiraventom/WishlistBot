using Serilog;
using WishlistBot.Database.Users;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Parameters;

namespace WishlistBot.BotMessages.EditWish;

public class CancelEditWishMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
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
            .AddButton<CompactListQuery>("Назад к моим вишам");
      }

      user.CurrentWish = null;
   }
}
