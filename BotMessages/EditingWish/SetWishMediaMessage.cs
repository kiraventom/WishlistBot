using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;
using System.Text;

namespace WishlistBot.BotMessages.EditingWish;

public class SetWishMediaMessage : BotMessage
{
   public SetWishMediaMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, IReadOnlyCollection<string> parameters = null)
   {
      Keyboard = new BotKeyboard();

      if (user.CurrentWish.FileId is not null)
         Keyboard.AddButton<EditWishQuery>("Удалить", "clearMedia");

      Keyboard.AddButton<EditWishQuery>("Отмена");

      PhotoFileId = user.CurrentWish.FileId;
      var text = "Пришлите фото виша:";
      if (PhotoFileId is not null)
         text = "Пришлите новое фото или удалите текущее";

      Text = text;

      user.BotState = BotState.SettingWishMedia;
   }
}
