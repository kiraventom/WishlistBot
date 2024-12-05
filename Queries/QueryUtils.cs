using System.Text;
using WishlistBot.Queries.Parameters;

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
