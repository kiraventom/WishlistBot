namespace WishlistBot.Queries.Subscription;

public class MySubscriptionsQuery : IQuery
{
   private const string subscriptionsEmoji = "\U0001f5c2\ufe0f";
   public string Caption => $"{subscriptionsEmoji} Подписки";
   public string Data => "@my_subscriptions";
}
