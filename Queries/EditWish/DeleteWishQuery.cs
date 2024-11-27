namespace WishlistBot.Queries.EditWish;

public class DeleteWishQuery : IQuery
{
   public string Caption => "Да, удалить \u2714\ufe0f";
   public string Data => "@delete_wish";
}
