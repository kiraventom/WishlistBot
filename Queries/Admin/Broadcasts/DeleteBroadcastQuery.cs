namespace WishlistBot.Queries.Admin.Broadcasts;

public class DeleteBroadcastQuery : IQuery
{
   public string Caption => "Delete broadcast";
   public string Data => "@admin_del_broadcast";
}

