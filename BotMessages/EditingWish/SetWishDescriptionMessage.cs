using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;
using System.Text;

namespace WishlistBot.BotMessages.EditingWish;

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

      var stringBuilder = new StringBuilder();

      if (user.CurrentWish.Description is null)
      {
         stringBuilder.AppendLine("Укажите подробное описание виша:");

      }
      else
      {
         // TODO: Description in Markdown monospace
         stringBuilder
            .AppendLine("Текущее описание виша:")
            .AppendLine(user.CurrentWish.Description)
            .AppendLine()
            .AppendLine("Укажите новое описание виша:");
      }

      Text = stringBuilder.ToString();

      user.BotState = BotState.SettingWishDescription;
   }
}
