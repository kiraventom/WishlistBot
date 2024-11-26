namespace WishlistBot.Queries.EditWish;

public class DeleteWishQuery : IQuery
{
   public string Caption => "Удалить \U0001f5d1\U0000fe0f";
   public string Data => "@delete_wish";
}
