using System.Globalization;
using System.Text;

namespace WishlistBot.QueryParameters;

public readonly struct QueryParameter(QueryParameterType type, long? value = null)
{
   private const char EQUALS_CH = '=';

   public static QueryParameter ForceNewWish { get; } = new(QueryParameterType.ForceNewWish);
   public static QueryParameter ReturnToMyClaims { get; } = new(QueryParameterType.ReturnToMyClaims);
   public static QueryParameter ReturnToFriends { get; } = new(QueryParameterType.ReturnToFriends);
   public static QueryParameter ClaimWish { get; } = new(QueryParameterType.ClaimWish);
   public static QueryParameter ForceNewMessage { get; } = new(QueryParameterType.ForceNewMessage);
   public static QueryParameter CancelJob { get; } = new(QueryParameterType.CancelJob);
   public static QueryParameter SaveDraft { get; } = new(QueryParameterType.SaveDraft);
   public static QueryParameter ChangeWishSortOrder { get; } = new(QueryParameterType.ChangeWishSortOrder);
   public static QueryParameter ChangeWishSortProperty { get; } = new(QueryParameterType.ChangeWishSortProperty);
   public static QueryParameter WishFilterToggleUnclaimed { get; } = new(QueryParameterType.WishFilterToggleUnclaimed);
   public static QueryParameter ToggleWishPermanence { get; } = new(QueryParameterType.ToggleWishPermanence);
   public static QueryParameter ShowHelp { get; } = new(QueryParameterType.ShowHelp);

   public QueryParameterType Type { get; } = type;
   public long? Value { get; } = value;

   public static bool TryParse(string str, out QueryParameter parameter)
   {
      parameter = default;

      var parts = str.Split(EQUALS_CH);
      if (parts.Length is < 1 or > 2)
         return false;

      // Both the type and the value (when present) must be valid,
      // otherwise the parameter would silently turn into an empty one
      if (!int.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var type)
          || !Enum.IsDefined((QueryParameterType)type))
         return false;

      if (parts.Length == 1)
      {
         parameter = new QueryParameter((QueryParameterType)type);
         return true;
      }

      if (!long.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
         return false;

      parameter = new QueryParameter((QueryParameterType)type, value);
      return true;
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
