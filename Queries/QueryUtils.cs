using System.Text;
using WishlistBot.QueryParameters;

namespace WishlistBot.Queries;

public static class QueryUtils
{
   private const char DATA_SEPARATOR = '?';

   public static string BuildQueryStr(string data, QueryParameterCollection parameters)
   {
      var stringBuilder = new StringBuilder();
      stringBuilder.Append(data);

      if (parameters.Count > 0)
      {
         stringBuilder.Append(DATA_SEPARATOR);
         stringBuilder.Append(parameters);
      }

      return stringBuilder.ToString();
   }

   /// <summary>
   /// Telegram хранит данные tg-кнопки как HTML-атрибут: амперсанд в data="" остаётся
   /// экранированным (&amp;amp;) и в callback_query приходит именно так — проверено вживую,
   /// «сырой» и уже экранированный вариант сохраняются одинаково. Сверка с AllowedQueries
   /// и разбор параметров идут по расэкранированному виду, поэтому здесь обратное
   /// преобразование сущностей. Обычные кнопки клавиатуры приходят как есть — сущностей
   /// в них нет, функция их не трогает.
   /// </summary>
   public static string NormalizeCallbackData(string data)
   {
      if (string.IsNullOrEmpty(data) || !data.Contains('&'))
         return data;

      // Порядок важен: сначала остальные сущности, потом '&amp;' — иначе из '&amp;lt;'
      // получился бы '<' вместо '&lt;'
      return data
         .Replace("&quot;", "\"")
         .Replace("&lt;", "<")
         .Replace("&gt;", ">")
         .Replace("&amp;", "&");
   }

   public static bool TryParseQueryStr(string queryStr, out string name, out QueryParameterCollection parameters)
   {
      var parts = queryStr.Split(DATA_SEPARATOR);

      switch (parts.Length)
      {
         case 2:
            name = parts[0];
            var queryParamsStr = parts[1];
            return QueryParameterCollection.TryParse(queryParamsStr, out parameters);
         case 1:
            name = parts[0];
            parameters = new QueryParameterCollection();
            return true;
         default:
            name = null;
            parameters = null;
            return false;
      }
   }
}
