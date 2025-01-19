using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class AllowedTypesAttribute(params QueryParameterType[] allowedTypes) : Attribute
{
   public IReadOnlyCollection<QueryParameterType> AllowedTypes { get; } = allowedTypes;
}
