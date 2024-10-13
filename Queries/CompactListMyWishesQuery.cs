namespace WishlistBot.Queries;

public class CompactListMyWishesQuery : IQuery
{
   public string Caption => "Краткий список";
   public string Data => "@compact_list_my_wishes";
}
