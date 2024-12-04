using Telegram.Bot.Types.ReplyMarkups;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;

namespace WishlistBot.Keyboard;

public class BotButton(string data, string caption, IEnumerable<QueryParameter> parameters)
{
   private QueryParameterCollection Parameters { get; } = new(parameters);

   public InlineKeyboardButton ToInlineKeyboardButton(QueryParameterCollection commonParameters)
   {
      var mergedParameters = QueryParameterCollection.Merge(commonParameters, Parameters);
      var dataWithParameters = QueryUtils.BuildQueryStr(data, mergedParameters);
      return new InlineKeyboardButton { Text = caption, CallbackData = dataWithParameters };
   }
}
