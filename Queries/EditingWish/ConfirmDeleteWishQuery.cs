namespace WishlistBot.Queries.EditingWish;

public class ConfirmDeleteWishQuery : IQuery
{
   public string Caption => "Да, удалить \u2714\ufe0f";
   public string Data => "@confirm_delete_wish";
}
