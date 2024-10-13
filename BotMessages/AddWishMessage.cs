using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class AddWishMessage : BotMessage
{
   public AddWishMessage(ILogger logger, BotUser user) : base(logger)
   {
      Keyboard = new BotKeyboard();

      Text = "Добавление виша\n\nПришлите фотографии, текст или ссылки, относящиеся к новому вишу, и отправьте команду /save.\n\nДля отмены отправьте команду /cancel";

      user.BotState = BotState.AddingWish;
      user.CurrentWish = new Wish();
   }
}
