namespace WishlistBot.QueryParameters;

[Flags]
public enum SettingsEnum
{
   None = 0x0,
   SendNotifications = 0x1,
   ReceiveFriendWishNotifications = 0x2,
   BirthdayNotifications = 0x4,
   WishlistCleanupNotifications = 0x8,
   WishlistFillNotifications = 0x10,
}
