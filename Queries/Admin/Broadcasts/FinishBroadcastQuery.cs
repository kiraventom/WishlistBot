namespace WishlistBot.Queries.Admin.Broadcasts;

public class FinishBroadcastQuery : IQuery
{
   public string Caption => "Finish broadcast";
   public string Data => "@admin_finish_broadcast";
}
