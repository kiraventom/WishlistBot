using System.Text; 
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Queries;

namespace WishlistBot.Keyboard;

public class BotKeyboard
{
   private readonly List<List<BotButton>> _rows = new();

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
         lastRow = new List<BotButton>();
         _rows.Add(lastRow);
      }

      var button = new BotButton(data, caption);
      lastRow.Add(button);
      return this;

   }

   public BotKeyboard NewRow()
   {
      _rows.Add(new List<BotButton>());
      return this;
   }

   public InlineKeyboardMarkup ToInlineKeyboardMarkup()
   {
      var markupRows = _rows
         .Where(r => r.Any())
         .Select(r => r.Select(b => b.ToInlineKeyboardButton()));

      return new InlineKeyboardMarkup() { InlineKeyboard = markupRows }; 
   }
}
