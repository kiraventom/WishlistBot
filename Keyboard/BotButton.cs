using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace WishlistBot.Keyboard;

public class BotButton(string Data, string Caption)
{
   public InlineKeyboardButton ToInlineKeyboardButton()
   {
      return new InlineKeyboardButton { Text = Caption, CallbackData = Data };
   }
}
