using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

public class SetWishMediaMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters); 

      if (user.CurrentWish.FileId is not null)
         Keyboard.AddButton<EditWishQuery>("Удалить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Media));

      Keyboard
         .NewRow()
         .AddButton<EditWishQuery>("Отмена");

      PhotoFileId = user.CurrentWish.FileId;

      Text.Verbatim(PhotoFileId is not null ? "Пришлите новое фото или удалите текущее" : "Пришлите фото виша:");

      user.BotState = BotState.ListenForWishMedia;
   }
}
