using WishlistBot.Queries.Parameters;

namespace WishlistBot.BotMessages;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class AllowedTypesAttribute : Attribute
{
   public IReadOnlyCollection<QueryParameterType> AllowedTypes { get; }

   public AllowedTypesAttribute(params QueryParameterType[] allowedTypes)
   {
      AllowedTypes = allowedTypes;
   }
}
