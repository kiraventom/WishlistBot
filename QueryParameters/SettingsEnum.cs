namespace WishlistBot.QueryParameters;

[Flags]
public enum SettingsEnum
{
   None = 0x0,
   SendNotifications = 0x1,
   ReceiveNotifications = 0x2,
}
