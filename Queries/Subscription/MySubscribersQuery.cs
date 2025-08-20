namespace WishlistBot.Queries.Subscription;

public class MySubscribersQuery : IQuery
{
   private const string subscribersEmoji = "\U0001f465";
   public string Caption => $"{subscribersEmoji} Подписчики";
   public string Data => "@my_subscribers";
}
