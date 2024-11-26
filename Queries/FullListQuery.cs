namespace WishlistBot.Queries;

public class FullListQuery : IQuery
{
   public string Caption => "Полный список";
   public string Data => "@full_list";
}
