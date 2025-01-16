namespace WishlistBot.Queries.Admin.Broadcasts;

public class DeleteBroadcastQuery : IAdminQuery
{
   public string Caption => "Delete broadcast";
   public string Data => "@admin_del_broadcast";
}

