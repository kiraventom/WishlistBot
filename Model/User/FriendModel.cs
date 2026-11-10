using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

/// <summary>
/// Дружба симметрична и хранится зеркальными парами строк: для каждой дружбы
/// есть запись «A — друг B» и «B — друг A». Благодаря этому список друзей
/// читается одним запросом по UserId, без OR по двум колонкам, а инвариант
/// «связь видна обоим» держится на том, что строки всегда добавляются и
/// удаляются парой (см. <see cref="FriendshipManager"/>).
/// </summary>
[Index(nameof(UserId), nameof(FriendId), IsUnique = true)]
public class FriendModel
{
    [Key] public int FriendshipId { get; set; }

    /// <summary>Владелец записи: чей это список друзей.</summary>
    public int UserId { get; set; }

    /// <summary>Пользователь, который находится в списке друзей владельца.</summary>
    public int FriendId { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(UserModel.Friends))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    [ForeignKey(nameof(FriendId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Friend { get; set; }
}
