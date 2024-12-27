using System.Globalization;
using System.Text;

namespace WishlistBot.Queries.Parameters;

public readonly struct QueryParameter(QueryParameterType type, long? value = null)
{
   private const char EQUALS_CH = '=';

   public static QueryParameter ForceNewWish { get; } = new(QueryParameterType.ForceNewWish);
   public static QueryParameter ReturnToFullList { get; } = new(QueryParameterType.ReturnToFullList);
   public static QueryParameter ReadOnly { get; } = new(QueryParameterType.ReadOnly);
   public static QueryParameter ReturnToSubscriber { get; } = new(QueryParameterType.ReturnToSubscriber);
   public static QueryParameter ClaimWish { get; } = new(QueryParameterType.ClaimWish);
   public static QueryParameter ForceNewMessage { get; } = new(QueryParameterType.ForceNewMessage);

   public QueryParameterType Type { get; } = type;
   public long? Value { get; } = value;

   public static bool TryParse(string str, out QueryParameter parameter)
   {
      int type = default;
      long value = default;

      var parts = str.Split(EQUALS_CH);

      var isValidValue = parts.Length == 2 && 
         long.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

      var isValidType = parts.Length >= 1 && 
         int.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out type) && 
         Enum.IsDefined((QueryParameterType)type);

      parameter = isValidType switch
      {
         true when isValidValue => new QueryParameter((QueryParameterType)type, value),
         true => new QueryParameter((QueryParameterType)type),
         _ => default
      };

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
