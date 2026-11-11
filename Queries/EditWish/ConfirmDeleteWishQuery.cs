namespace WishlistBot.Queries.EditWish;

public class ConfirmDeleteWishQuery : IQuery
{
   public string Caption => "\U0001f5d1\ufe0f Удалить";
   public string Data => "@confirm_delete_wish";
}
