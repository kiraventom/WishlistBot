namespace WishlistBot.Queries.Parameters;

public enum QueryParameterType
{
   ForceNewWish = 0x1,
   ClearWishProperty = 0x2,
   SetCurrentWishTo = 0x3,
   ReturnToFullList = 0x4,
   SetListPageTo = 0x7,
   ReadOnly = 0x8,
   SetUserTo = 0x9,
   ReturnToSubscriber = 0xA,
}
