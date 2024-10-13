namespace WishlistBot.Queries;

public class AddWishQuery : IQuery
{
   public string Caption => "Добавить виш";
   public string Data => "@add_wish";
}
