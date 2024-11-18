namespace WishlistBot.Queries.EditingWish;

public class FinishEditingWishQuery : IQuery
{
   public string Caption => "Готово";
   public string Data => "@finish_editing_wish";
}
