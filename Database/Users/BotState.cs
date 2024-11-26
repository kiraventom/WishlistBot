namespace WishlistBot.Database.Users;

public enum BotState
{
   WaitingForStart, MainMenu, MyWishes, MySubscriptions, Settings,
   EditingWish, SettingWishName, SettingWishDescription, SettingWishMedia, SettingWishLinks,
   WishAdded, 
   CompactList, 
   EditingList,
   DeletingWish, WishDeleted
}
