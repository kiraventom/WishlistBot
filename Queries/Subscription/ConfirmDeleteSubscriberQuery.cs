namespace WishlistBot.Queries.Subscription;

public class ConfirmDeleteSubscriberQuery : IQuery
{
   public string Caption => "Удалить";
   public string Data => "@confirm_del_subscriber";
}
