namespace WishlistBot.Database.Users;

public enum BotState
{
   WaitingForStart, MainMenu, MyWishes, MySubscriptions, Settings,
   EditWish, SettingWishName, SettingWishDescription, SettingWishMedia, SettingWishLinks,
   WishAdded, 
   CompactList, 
   EditingList,
   DeletingWish, WishDeleted
}
