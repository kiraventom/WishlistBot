namespace WishlistBot.Queries.Parameters;

public enum QueryParameterType : byte
{
   ForceNewWish = 0x1,
   ClearWishMedia = 0x2,
   SetCurrentWishTo = 0x3,
   ReturnToEditList = 0x4,
}
