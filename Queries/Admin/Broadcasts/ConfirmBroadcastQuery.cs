namespace WishlistBot.Queries.Admin.Broadcasts;

public class ConfirmBroadcastQuery : IAdminQuery
{
   public string Caption => "Confirm broadcast";
   public string Data => "@admin_c_broadcast";
}

