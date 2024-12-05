namespace WishlistBot.BotMessages;

[AttributeUsage(AttributeTargets.Class)]
public class ChildMessageAttribute : Attribute
{
   public Type ParentMessageType { get; }

   public ChildMessageAttribute(Type parentMessageType)
   {
      ParentMessageType = parentMessageType;
   }
}
