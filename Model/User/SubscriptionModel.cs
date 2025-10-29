using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

[Index(nameof(SubscriberId), nameof(TargetId), IsUnique=true)]
public class SubscriptionModel
{
    [Key] public int SubscriptionId { get; set; }
    public int SubscriberId { get; set; }
    public int TargetId { get; set; }

    [ForeignKey(nameof(SubscriberId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Subscriber { get; set; }

    [ForeignKey(nameof(TargetId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Target { get; set; }
}

