using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.QueryParameters;

namespace WishlistBot.Keyboard;

public class CopyTextBotButton(string caption, string textToCopy) : IBotButton
{
    public InlineKeyboardButton ToInlineKeyboardButton(QueryParameterCollection commonParameters)
    {
        return new InlineKeyboardButton() { Text = caption, CopyText = new CopyTextButton() { Text = textToCopy } };
    }
}

