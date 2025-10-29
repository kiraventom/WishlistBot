using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class ListPositionsModel : IViewerTarget
{
    [Key]
    public int ListPositionModelId { get; set; }

    public int TargetId { get; set; }
    public int ViewerId { get; set; }

    [ForeignKey(nameof(TargetId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Target { get; set; }

    [ForeignKey(nameof(ViewerId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Viewer { get; set; }

    public int WishPage { get; set; }
    public int SubscriberPage { get; set; }
    public int SubscriptionPage { get; set; }
    public int ClaimPage { get; set; }
    public int AdminBroadcastPage { get; set; }
    public int AdminUserPage { get; set; }
}

