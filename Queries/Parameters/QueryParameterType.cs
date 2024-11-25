namespace WishlistBot.Queries.Parameters;

public enum QueryParameterType : int
{
   ForceNewWish = 0x1,
   ClearWishProperty = 0x2,
   SetCurrentWishTo = 0x3,
   ReturnToEditList = 0x4,
   ReturnToCompactList = 0x5,
   SetListPageTo = 0x7,
}
