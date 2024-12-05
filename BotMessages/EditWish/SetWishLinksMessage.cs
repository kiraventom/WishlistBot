using Serilog;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
public class SetWishLinksMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
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

         for (var i = 0; i < user.CurrentWish.Links.Count; ++i)
         {
            var link = user.CurrentWish.Links[i];
            Text.LineBreak().InlineUrl($"Ссылка {i + 1}", link);
         }

         Text.Verbatim("Пришлите новые ссылки или удалите текущие:");
      }

      user.BotState = BotState.ListenForWishLinks;
   }
}
