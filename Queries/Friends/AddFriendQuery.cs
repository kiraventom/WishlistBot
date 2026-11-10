namespace WishlistBot.Queries.Friends;

public class AddFriendQuery : IQuery
{
   public string Caption => "Добавить в друзья";
   public string Data => "@add_friend";
}
