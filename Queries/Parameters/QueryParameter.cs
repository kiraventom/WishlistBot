using System.Globalization;
using System.Text;

namespace WishlistBot.Queries.Parameters;

public struct QueryParameter
{
   private const char EQUALS_CH = '=';

   public static QueryParameter ForceNewWish { get; } = new QueryParameter(QueryParameterType.ForceNewWish);
   public static QueryParameter ReturnToFullList { get; } = new QueryParameter(QueryParameterType.ReturnToFullList);

   public QueryParameterType Type { get; }
   public int? Value { get; }

   public QueryParameter(QueryParameterType type, int? value = null)
   {
      Type = type;
      Value = value;
   }
   
   public static bool TryParse(string str, out QueryParameter parameter)
   {
      int type = default;
      int value = default;

      var parts = str.Split(EQUALS_CH);

      var isValidValue = parts.Length == 2 && 
         int.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

      var isValidType = parts.Length >= 1 && 
         int.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out type) && 
         Enum.IsDefined<QueryParameterType>((QueryParameterType)type);

      if (isValidType && isValidValue)
         parameter = new QueryParameter((QueryParameterType)type, value);
      else if (isValidType)
         parameter = new QueryParameter((QueryParameterType)type, null);
      else
         parameter = default;

      return isValidType || isValidValue;
   }

   public override string ToString()
   {
      var stringBuilder = new StringBuilder();
      stringBuilder.Append(((int)Type).ToString("X"));
      if (Value.HasValue)
         stringBuilder.Append(EQUALS_CH).Append(Value.Value.ToString("X"));

      return stringBuilder.ToString();
   }
}
