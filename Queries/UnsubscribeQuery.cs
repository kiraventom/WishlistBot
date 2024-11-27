namespace WishlistBot.Queries;

public class UnsubscribeQuery : IQuery
{
   public string Caption => "Отписаться";
   public string Data => "@unsubscribe";
}
