using WishlistBot.Model.User;

namespace WishlistBot.BotMessages;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class AllowedListPositionsAttribute(params ListPosition[] allowedPositions) : Attribute
{
   public IReadOnlyCollection<ListPosition> AllowedPositions { get; } = allowedPositions;
}

