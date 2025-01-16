namespace WishlistBot.Queries.Admin.Broadcasts;

public class ConfirmDeleteBroadcastQuery : IQuery
{
   public string Caption => "Confirm delete";
   public string Data => "@admin_c_del_broadcast";
}
