namespace WishlistBot.Queries.Subscription;

public class FinishSubscriptionQuery : IQuery
{
   public string Caption => "Подписаться";
   public string Data => "@subscribe";
}
