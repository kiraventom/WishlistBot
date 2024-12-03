namespace WishlistBot.Queries.Subscription;

public class ConfirmUnsubscribeQuery : IQuery
{
   public string Caption => "Отписаться";
   public string Data => "@confirm_unsubscribe";
}
