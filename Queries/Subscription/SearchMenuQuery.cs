namespace WishlistBot.Queries;

public class SearchMenuQuery : IQuery
{
   private const string magnifierEmoji = "\U0001f50d";
   public string Caption => $"{magnifierEmoji} Поиск";
   public string Data => "@search_menu";
}
