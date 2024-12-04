namespace WishlistBot.Queries.Subscription;

public class DeleteSubscriberQuery : IQuery
{
   public string Caption => "Да, удалить";
   public string Data => "@del_subscriber";
}
