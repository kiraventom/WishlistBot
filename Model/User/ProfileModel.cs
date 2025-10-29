using System.ComponentModel.DataAnnotations;

namespace WishlistBot.Model.User;

public class ProfileModel
{
    [Key] public int ProfileId { get; set; }

    public int UserId { get; set; }
    public UserModel User { get; set; }

    public DateOnly? Birthday { get; set; }
    public string Notes { get; set; }

    public bool IsPublic { get; set; }
}

