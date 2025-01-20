namespace WishlistBot.Queries.Subscription;

public class MySubscriptionsQuery : IQuery
{
   private const string subscriptionsEmoji = "\U0001f465";
   public string Caption => $"{subscriptionsEmoji} Мои подписки";
   public string Data => "@my_subscriptions";
}
