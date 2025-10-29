using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class UserExtraModel
{
    [Key]
    public int UserExtraModelId { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    public bool KeyboardCleaned => LastCleanedMessageId < 0;
    public int? LastCleanedMessageId { get; set; }
}

