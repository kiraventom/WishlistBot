using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Queries;

namespace WishlistBot.Keyboard;

public class BotKeyboard
{
   private readonly List<Dictionary<string, string>> _rows = new();

   public BotKeyboard AddButton<T>(string customCaption = null) where T : IQuery, new()
   {
      var query = new T();
      var caption = customCaption ?? query.Caption;
      return AddButton(query.Data, caption);
   }

   public BotKeyboard AddButton(string data, string caption)
   {
      var lastRow = _rows.LastOrDefault();
      if (lastRow is null)
      {
         lastRow = new Dictionary<string, string>();
         _rows.Add(lastRow);
      }

      lastRow.Add(data, caption);
      return this;

   }

   public BotKeyboard NewRow()
   {
      _rows.Add(new Dictionary<string, string>());
      return this;
   }

   public InlineKeyboardMarkup ToInlineKeyboardMarkup()
   {
      var markupRows = _rows
         .Where(r => r.Any())
         .Select(r => r.Select(p => new InlineKeyboardButton()
                  { Text = p.Value, CallbackData = p.Key }));

      return new InlineKeyboardMarkup() { InlineKeyboard = markupRows }; 
   }
}
