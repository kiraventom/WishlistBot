using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Queries;
using WishlistBot.QueryParameters;

namespace WishlistBot.Keyboard;

public class BotKeyboard()
{
   private readonly List<List<BotButton>> _rows = [];
   private QueryParameterCollection _commonParameters = new();

   public void InitCommonParameters(QueryParameterCollection commonParameters) => _commonParameters = commonParameters;

   public BotKeyboard AddButton<T>(string customCaption = null) where T : IQuery, new() => AddButton<T>(customCaption, []);

   public BotKeyboard AddButton<T>(params QueryParameter[] parameters) where T : IQuery, new() => AddButton<T>(null, parameters);

   public BotKeyboard AddButton<T>(string customCaption, params QueryParameter[] parameters) where T : IQuery, new()
   {
      var query = new T();
      var caption = customCaption ?? query.Caption;

      return AddButton(query.Data, caption, parameters);
   }

   public BotKeyboard AddButton(string data, string caption, IEnumerable<QueryParameter> parameters = null)
   {
      var lastRow = _rows.LastOrDefault();
      if (lastRow is null)
      {
         lastRow = [];
         _rows.Add(lastRow);
      }

      parameters ??= [];

      var button = new BotButton(data, caption, parameters);
      lastRow.Add(button);
      return this;
   }

   public BotKeyboard NewRow()
   {
      _rows.Add([]);
      return this;
   }

   public InlineKeyboardMarkup ToInlineKeyboardMarkup()
   {
      var markupRows = _rows
         .Where(r => r.Count != 0)
         .Select(r => r.Select(b => b.ToInlineKeyboardButton(_commonParameters)));

      return new InlineKeyboardMarkup() { InlineKeyboard = markupRows };
   }
}
