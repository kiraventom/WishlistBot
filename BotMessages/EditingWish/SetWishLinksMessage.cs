using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;
using System.Text;

namespace WishlistBot.BotMessages.EditingWish;

public class SetWishLinksMessage : BotMessage
{
   public SetWishLinksMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard()
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
