using System.Collections;
using System.Text;

namespace WishlistBot.QueryParameters;

public class QueryParameterCollection : IEnumerable<QueryParameter>
{
   private const char PARAM_SEPARATOR = '&';

   private readonly Dictionary<QueryParameterType, QueryParameter> _parameters;

   public int Count => _parameters.Count;

   public QueryParameterCollection()
   {
      _parameters = new Dictionary<QueryParameterType, QueryParameter>();
   }

   public QueryParameterCollection(IEnumerable<QueryParameter> parameters)
   {
      _parameters = parameters.ToDictionary(p => p.Type, p => p);
   }

   private void Push(QueryParameter parameter) => _parameters[parameter.Type] = parameter;

   public bool Peek(QueryParameterType type) => _parameters.ContainsKey(type);

   public bool Peek(QueryParameterType type, out long value)
   {
      if (_parameters.TryGetValue(type, out var parameter))
      {
         if (parameter.Value.HasValue)
         {
            value = parameter.Value.Value;
            return true;
         }
      }

      value = int.MinValue;
      return false;
   }

   public bool Pop(QueryParameterType type)
   {
      var found = Peek(type);
      if (found)
         _parameters.Remove(type);

      return found;
   }

   public bool Pop(QueryParameterType type, out long value)
   {
      var found = Peek(type, out value);
      if (found)
         _parameters.Remove(type);

      return found;
   }

   public static bool TryParse(string queryParamsStr, out QueryParameterCollection parameters)
   {
      parameters = new QueryParameterCollection();

      if (queryParamsStr is null)
         return false;

      var queryParams = queryParamsStr.Split(PARAM_SEPARATOR);

      foreach (var queryParam in queryParams)
      {
         if (!QueryParameter.TryParse(queryParam, out var parameter))
            return false;

         parameters.Push(parameter);
      }

      return true;
   }

   public static QueryParameterCollection Merge(QueryParameterCollection col0, QueryParameterCollection col1)
   {
      var merged = new QueryParameterCollection();
      foreach (var p in col0)
         merged.Push(p);

      foreach (var p in col1)
         merged.Push(p);

      return merged;
   }

   public IEnumerator<QueryParameter> GetEnumerator() => _parameters.Values.GetEnumerator();

   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   public override string ToString()
   {
      if (_parameters.Count == 0)
         return string.Empty;

      var stringBuilder = new StringBuilder();
      foreach (var parameter in _parameters.Values)
      {
         stringBuilder.Append(parameter.ToString());
         stringBuilder.Append(PARAM_SEPARATOR);
      }

      stringBuilder.Remove(stringBuilder.Length - 1, 1);

      return stringBuilder.ToString();
   }
}
