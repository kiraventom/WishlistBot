namespace WishlistBot.Queries;

public class FullListMyWishesQuery : IQuery
{
   public string Caption => "Полный список";
   public string Data => "@full_list_my_wishes";
}
