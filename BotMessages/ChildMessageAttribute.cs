namespace WishlistBot.BotMessages;

[AttributeUsage(AttributeTargets.Class)]
public class ChildMessageAttribute(Type parentMessageType) : Attribute
{
   public Type ParentMessageType { get; } = parentMessageType;

}
