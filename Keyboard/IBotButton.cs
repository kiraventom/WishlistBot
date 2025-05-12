using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.QueryParameters;

namespace WishlistBot.Keyboard;

public interface IBotButton
{
   InlineKeyboardButton ToInlineKeyboardButton(QueryParameterCollection commonParameters);
}

