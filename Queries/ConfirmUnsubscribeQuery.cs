namespace WishlistBot.Queries;

public class ConfirmUnsubscribeQuery : IQuery
{
   public string Caption => "Отписаться";
   public string Data => "@confirm_unsubscribe";
}
