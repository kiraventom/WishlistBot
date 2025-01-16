namespace WishlistBot.Queries.Admin.Broadcasts;

public class FinishBroadcastQuery : IAdminQuery
{
   public string Caption => "Finish broadcast";
   public string Data => "@admin_finish_broadcast";
}
