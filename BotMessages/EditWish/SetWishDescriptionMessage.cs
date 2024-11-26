using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using System.Text;

namespace WishlistBot.BotMessages.EditWish;

public class SetWishDescriptionMessage : BotMessage
{
   public SetWishDescriptionMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
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

      user.BotState = BotState.SettingWishDescription;
   }
}
