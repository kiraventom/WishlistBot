namespace WishlistBot.Queries.Friends;

public class MyFriendsQuery : IQuery
{
   private const string friendsEmoji = "\U0001f465";
   public string Caption => $"{friendsEmoji} Друзья";
   public string Data => "@my_friends";
}
