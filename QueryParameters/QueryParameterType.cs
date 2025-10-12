namespace WishlistBot.QueryParameters;

public enum QueryParameterType
{
   ForceNewWish = 0x1,
   ClearWishProperty = 0x2,
   WishId = 0x3,
   // ReturnToFullList = 0x4, OBSOLETE
   SetListPageTo = 0x7,
   // ReadOnly = 0x8, OBSOLETE
   UserId = 0x9,
   ReturnToSubscriber = 0xA,
   ClaimWish = 0xB,
   ForceNewMessage = 0xC,
   SetPriceTo = 0xD,
   SetSettingsTo = 0xE,
   RegenerateLink = 0xF,
   ReturnToMyClaims = 0x10,
   // CleanDraft = 0x11, OBSOLETE
   SaveDraft = 0x12,
   ChangeProfileType = 0x13,
   ChangeWishSortOrder = 0x14,
   ChangeWishSortProperty = 0x15,
   WishFilterToggleUnclaimed = 0x16,

   // Admin
   SetBroadcastTo = -0x1,
   CancelJob = -0x2
}
