namespace WishlistBot.Queries;

public class FullListQuery : IQuery
{
   private const string listEmoji = "\U0001f4d1";
   public string Caption => $"{listEmoji} Полный список";
   public string Data => "@full_list";
}
