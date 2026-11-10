namespace WishlistBot.Queries.Friends;

public class RemoveFriendQuery : IQuery
{
   public string Caption => "Да, удалить";
   public string Data => "@remove_friend";
}
