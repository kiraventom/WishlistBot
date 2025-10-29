using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class ListPositionsModel
{
    [Key]
    public int ListPositionModelId { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    public int WishPage { get; set; }
    public int SubscriberPage { get; set; }
    public int SubscriptionPage { get; set; }
    public int ClaimPage { get; set; }
    public int AdminBroadcastPage { get; set; }
    public int AdminUserPage { get; set; }
}

