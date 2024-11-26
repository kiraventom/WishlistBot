namespace WishlistBot.Queries.EditingWish;

public class FinishEditWishQuery : IQuery
{
   public string Caption => "Готово";
   public string Data => "@finish_edit_wish";
}
