using System.Globalization;
using System.Text;

namespace WishlistBot.Queries;

public enum QueryParameterType : byte
{
   ForceNewWish = 0x1,
   ClearWishMedia = 0x2,
   SetCurrentWishTo = 0x3,
   ReturnToEditList = 0x4,
}

public struct QueryParameter
{
   private const char EQUALS_CH = '=';

   public static QueryParameter ForceNewWish { get; } = new QueryParameter(QueryParameterType.ForceNewWish);
   public static QueryParameter ClearWishMedia { get; } = new QueryParameter(QueryParameterType.ClearWishMedia);
   public static QueryParameter ReturnToEditList { get; } = new QueryParameter(QueryParameterType.ReturnToEditList);

   public byte Type { get; }
   public byte? Value { get; }

   public QueryParameter(QueryParameterType type, byte? value = null)
   {
      Type = (byte)type;
      Value = value;
   }
   
   public static bool TryParse(string str, out QueryParameter parameter)
   {
      bool isValid;
      byte type = byte.MaxValue;
      byte value = byte.MaxValue; // TODO FIx

      var parts = str.Split(EQUALS_CH);

      isValid = parts.Length == 2 && 
         byte.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

      isValid = parts.Length >= 1 && 
         byte.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out type) && 
         Enum.IsDefined<QueryParameterType>((QueryParameterType)type);

      if (isValid)
         parameter = new QueryParameter((QueryParameterType)type, value);
      else
         parameter = default;

      return isValid;
   }

   public static bool TryParseQueryParams(IReadOnlyList<string> queryParams, out QueryParameter[] parameters)
   {
      if (queryParams is null)
      {
         parameters = Array.Empty<QueryParameter>();
         return false;
      }

      parameters = new QueryParameter[queryParams.Count];
      for (int i = 0; i < queryParams.Count; ++i)
      {
         var queryParam = queryParams[i];
         if (!QueryParameter.TryParse(queryParam, out var parameter))
            return false;

         parameters[i] = parameter;
      }

      return true;
   }

   public override string ToString()
   {
      var stringBuilder = new StringBuilder();
      stringBuilder.Append(Type.ToString("X"));
      if (Value.HasValue)
         stringBuilder.Append(EQUALS_CH).Append(Value.Value.ToString("X"));

      return stringBuilder.ToString();
   }
}
