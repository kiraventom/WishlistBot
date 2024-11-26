using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using System.Text;

namespace WishlistBot.BotMessages.EditWish;

public class SetWishMediaMessage : BotMessage
{
   public SetWishMediaMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters); 

      if (user.CurrentWish.FileId is not null)
         Keyboard.AddButton<EditWishQuery>("Удалить", new QueryParameter(QueryParameterType.ClearWishProperty, (int)WishPropertyType.Media));

      Keyboard
         .NewRow()
         .AddButton<EditWishQuery>("Отмена");

      PhotoFileId = user.CurrentWish.FileId;

      if (PhotoFileId is not null)
         Text.Verbatim("Пришлите новое фото или удалите текущее");
      else
         Text.Verbatim("Пришлите фото виша:");

      user.BotState = BotState.SettingWishMedia;
   }
}
