namespace WishlistBot.Queries.EditingWish;

public class ClearWishMediaQuery : IQuery
{
   public string Caption => "Удалить фото";
   public string Data => "@editing_wish_clear_media";
}
