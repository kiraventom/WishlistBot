using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database.Users;
using System.Text;

namespace WishlistBot.BotMessages.EditingWish;

public class SetWishLinksMessage : BotMessage
{
   public SetWishLinksMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (user.CurrentWish.Links.Any())
         Keyboard.AddButton<EditWishQuery>("Очистить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Links));

      Keyboard
         .NewRow()
         .AddButton<EditWishQuery>("Отмена");

      var stringBuilder = new StringBuilder();

      if (user.CurrentWish.Links is null)
      {
         stringBuilder.AppendLine("Пришлите ссылки на товары (одним сообщением):");

      }
      else
      {
         // TODO: Links as Markdown
         stringBuilder
            .AppendLine("Текущие ссылки: ")
            .Append(user.CurrentWish.Links.Count).AppendLine()
            .AppendLine()
            .AppendLine("Пришлите новые ссылки (одним сообщением):");
      }

      Text = stringBuilder.ToString();

      user.BotState = BotState.SettingWishLinks;
   }
}
