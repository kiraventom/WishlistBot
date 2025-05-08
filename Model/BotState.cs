namespace WishlistBot.Model;

public enum BotState
{
   Default = 0x0,

   // Wish editing
   ListenForWishName = 0x1,
   ListenForWishDescription = 0x2,
   ListenForWishMedia = 0x3,
   ListenForWishLinks = 0x4,

   // Admin
   ListenForBroadcast = -0x1
}
