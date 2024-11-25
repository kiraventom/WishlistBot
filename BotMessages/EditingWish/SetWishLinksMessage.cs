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

      if (user.CurrentWish.Links is null)
      {
         Text.Verbatim("Пришлите ссылки одним сообщением:");
      }
      else
      {
         Text.Bold("Текущие ссылки: ");

         for (int i = 0; i < user.CurrentWish.Links.Count; ++i)
         {
            var link = user.CurrentWish.Links[i];
            Text.LineBreak().InlineUrl($"Ссылка {i + 1}", link);
         }

         Text.Verbatim("Пришлите новые ссылки или удалите текущие:");
      }

      user.BotState = BotState.SettingWishLinks;
   }
}
