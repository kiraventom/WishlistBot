namespace WishlistBot.Queries.Subscription;

public class MySubscribersQuery : IQuery
{
   private const string subscribersEmoji = "\U0001f465";
   public string Caption => $"{subscribersEmoji} Мои подписчики";
   public string Data => "@my_subscribers";
}
