namespace WishlistBot.Queries.Subscription;

public class MySubscribersQuery : IQuery
{
   private const string subscribersEmoji = "\U0001fac2";
   public string Caption => $"{subscribersEmoji} Мои подписчики";
   public string Data => "@my_subscribers";
}
