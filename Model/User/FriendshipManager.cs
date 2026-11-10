using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

/// <summary>
/// Единственное место, где создаются и удаляются дружбы.
/// Строки в <see cref="FriendModel"/> зеркальные, поэтому обе стороны
/// всегда добавляются и удаляются вместе.
/// </summary>
public static class FriendshipManager
{
    public static bool IsFriendOf(this UserModel user, int otherUserId) =>
        user.Friends.Any(f => f.FriendId == otherUserId);

    public static bool IsFriend(UserContext userContext, int userId, int otherUserId) =>
        userContext.Friends.Any(f => f.UserId == userId && f.FriendId == otherUserId);

    public static void AddFriend(UserContext userContext, UserModel user, UserModel friend)
    {
        if (user.UserId == friend.UserId)
            return;

        var existing = GetRowsBetween(userContext, user.UserId, friend.UserId);

        AddRowIfMissing(userContext, existing, user, friend);
        AddRowIfMissing(userContext, existing, friend, user);
    }

    public static void RemoveFriend(UserContext userContext, UserModel user, UserModel friend)
    {
        if (user.UserId == friend.UserId)
            return;

        var rows = GetRowsBetween(userContext, user.UserId, friend.UserId);
        userContext.Friends.RemoveRange(rows);

        // Вишлисты друг другу больше не видны, поэтому снимаем брони в обе стороны
        userContext.Entry(user).Collection(u => u.ClaimedWishes).Load();
        userContext.Entry(friend).Collection(u => u.ClaimedWishes).Load();

        user.ClaimedWishes.RemoveAll(cw => cw.OwnerId == friend.UserId);
        friend.ClaimedWishes.RemoveAll(cw => cw.OwnerId == user.UserId);
    }

    private static List<FriendModel> GetRowsBetween(UserContext userContext, int userId, int otherUserId)
    {
        return userContext.Friends
            .Where(f => (f.UserId == userId && f.FriendId == otherUserId)
                     || (f.UserId == otherUserId && f.FriendId == userId))
            .ToList();
    }

    private static void AddRowIfMissing(UserContext userContext, List<FriendModel> existing, UserModel user, UserModel friend)
    {
        if (existing.Any(f => f.UserId == user.UserId && f.FriendId == friend.UserId))
            return;

        var row = new FriendModel
        {
            UserId = user.UserId,
            FriendId = friend.UserId,
            User = user,
            Friend = friend,
        };

        userContext.Friends.Add(row);
        existing.Add(row);
    }
}
