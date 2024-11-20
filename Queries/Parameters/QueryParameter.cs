using System.Globalization;
using System.Text;

namespace WishlistBot.Queries.Parameters;

public struct QueryParameter
{
   private const char EQUALS_CH = '=';

   public static QueryParameter ForceNewWish { get; } = new QueryParameter(QueryParameterType.ForceNewWish);
   public static QueryParameter ClearWishMedia { get; } = new QueryParameter(QueryParameterType.ClearWishMedia);
   public static QueryParameter ReturnToEditList { get; } = new QueryParameter(QueryParameterType.ReturnToEditList);

   public QueryParameterType Type { get; }
   public byte? Value { get; }

   public QueryParameter(QueryParameterType type, byte? value = null)
   {
      Type = type;
      Value = value;
   }
   
   public static bool TryParse(string str, out QueryParameter parameter)
   {
      byte type = default;
      byte value = default;

      var parts = str.Split(EQUALS_CH);

      var isValidValue = parts.Length == 2 && 
         byte.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

      var isValidType = parts.Length >= 1 && 
         byte.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out type) && 
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
      stringBuilder.Append(((byte)Type).ToString("X"));
      if (Value.HasValue)
         stringBuilder.Append(EQUALS_CH).Append(Value.Value.ToString("X"));

      return stringBuilder.ToString();
   }
}
