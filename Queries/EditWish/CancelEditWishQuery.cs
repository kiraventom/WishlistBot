namespace WishlistBot.Queries.EditWish;

public class CancelEditWishQuery : IQuery
{
   public string Caption => "Отмена";
   public string Data => "@cancel_edit_wish";
}
