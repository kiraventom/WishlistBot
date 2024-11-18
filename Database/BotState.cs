namespace WishlistBot.Database;

public enum BotState
{
   WaitingForStart, MainMenu, MyWishes, MySubscriptions, Settings,
   EditingWish, SettingWishName, SettingWishDescription, SettingWishMedia, SettingWishLinks,
   WishAdded, 
   CompactListMyWishes, FullListMyWishes,
   EditingList
}
