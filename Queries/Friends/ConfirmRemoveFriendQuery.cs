namespace WishlistBot.Queries.Friends;

public class ConfirmRemoveFriendQuery : IQuery
{
   public string Caption => "Удалить из друзей";
   public string Data => "@confirm_remove_friend";
}
