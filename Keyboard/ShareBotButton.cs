using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.QueryParameters;

namespace WishlistBot.Keyboard;

public class ShareBotButton(string caption, string textToShare) : IBotButton
{
    public InlineKeyboardButton ToInlineKeyboardButton(QueryParameterCollection commonParameters)
    {
        return InlineKeyboardButton.WithSwitchInlineQuery(caption, textToShare);
    }
}

