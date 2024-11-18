namespace WishlistBot.Queries.EditingWish;

public class CancelEditingWishQuery : IQuery
{
   public string Caption => "Отмена";
   public string Data => "@cancel_editing_wish";
}
