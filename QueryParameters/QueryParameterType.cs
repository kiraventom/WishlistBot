namespace WishlistBot.QueryParameters;

public enum QueryParameterType
{
   ForceNewWish = 0x1,
   ClearWishProperty = 0x2,
   SetWishTo = 0x3,
   ReturnToFullList = 0x4,
   SetListPageTo = 0x7,
   // ReadOnly = 0x8, OBSOLETE
   SetUserTo = 0x9,
   ReturnToSubscriber = 0xA,
   ClaimWish = 0xB,
   ForceNewMessage = 0xC,
   SetPriceTo = 0xD,
   SetSettingsTo = 0xE,
   RegenerateLink = 0xF,
   ReturnToMyClaims = 0x10,

   // Admin
   SetBroadcastTo = -0x1,
   CancelJob = -0x2
}
