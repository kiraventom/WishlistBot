namespace WishlistBot.Queries.Parameters;

public enum QueryParameterType : int
{
   ForceNewWish = 0x1,
   ClearWishProperty = 0x2,
   SetCurrentWishTo = 0x3,
   ReturnToFullList = 0x4,
   SetListPageTo = 0x7,
}
