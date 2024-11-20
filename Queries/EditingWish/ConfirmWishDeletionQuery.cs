namespace WishlistBot.Queries.EditingWish;

public class ConfirmWishDeletionQuery : IQuery
{
   public string Caption => "Да, удалить \u2714\ufe0f";
   public string Data => "@confirm_wish_deletion";
}
