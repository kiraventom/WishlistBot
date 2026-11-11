namespace WishlistBot.Queries.EditWish;

/// <summary>Подтверждение переключения типа виша.</summary>
public class ConfirmWishPermanenceQuery : IQuery
{
   public string Caption => "\u2705 Да";
   public string Data => "@confirm_wish_permanence";
}
