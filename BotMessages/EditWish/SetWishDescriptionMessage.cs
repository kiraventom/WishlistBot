using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

public class SetWishDescriptionMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (!string.IsNullOrEmpty(user.CurrentWish.Description))
         Keyboard.AddButton<EditWishQuery>("Очистить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Description));

      Keyboard
         .NewRow()
         .AddButton<EditWishQuery>("Отмена");

      if (user.CurrentWish.Description is null)
      {
         Text.Verbatim("Укажите подробное описание виша:");

      }
      else
      {
         Text
            .Bold("Текущее описание виша:")
            .LineBreak().Monospace(user.CurrentWish.Description)
            .LineBreak()
            .LineBreak().Verbatim("Укажите новое описание или удалите текущее:");
      }

      user.BotState = BotState.ListenForWishDescription;
   }
}
