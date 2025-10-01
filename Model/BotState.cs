namespace WishlistBot.Model;

public enum BotState
{
    Default = 0x0,

    // Wish editing
    ListenForWishName = 0x1,
    ListenForWishDescription = 0x2,
    ListenForWishMedia = 0x3,
    ListenForWishLinks = 0x4,

   // Profile editing
    ListenForBirthday = 0x5,
    ListerForProfileNotes = 0x6,

    // Search
    ListenForSearchQuery = 0x7,

    // Admin
    ListenForBroadcast = -0x1
}
