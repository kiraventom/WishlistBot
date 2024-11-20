using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Actions;

namespace WishlistBot.Keyboard;

public class BotButton
{
   public string Data { get; }
   public string Caption { get; }
   public QueryParameterCollection Parameters { get; }

   public BotButton(string data, string caption, IEnumerable<QueryParameter> parameters)
   {
      Data = data;
      Caption = caption;
      Parameters = new QueryParameterCollection(parameters);
   }

   public InlineKeyboardButton ToInlineKeyboardButton(QueryParameterCollection commonParameters)
   {
      var mergedParameters = QueryParameterCollection.Merge(commonParameters, Parameters);
      var dataWithParameters = QueryUtils.BuildQueryStr(Data, mergedParameters);
      return new InlineKeyboardButton { Text = Caption, CallbackData = dataWithParameters };
   }
}
