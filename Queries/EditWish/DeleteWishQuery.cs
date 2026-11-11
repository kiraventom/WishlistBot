namespace WishlistBot.Queries.EditWish;

public class DeleteWishQuery : IQuery
{
   public string Caption => "\u2714\ufe0f Да, удалить";
   public string Data => "@delete_wish";
}
