using WishlistBot.Queries.Parameters;

namespace WishlistBot.BotMessages;

public class AllowedTypesAttribute : Attribute
{
   public IReadOnlyCollection<QueryParameterType> AllowedTypes { get; }

   public AllowedTypesAttribute(params QueryParameterType[] allowedTypes)
   {
      AllowedTypes = allowedTypes;
   }
}
