using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public enum ListPosition { Wish, Subscriber, Subscription, Claim, AdminBroadcast, AdminUserPage }

public class ListPositionsModel
{
    [Key]
    public int ListPositionsId { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    public ListPositionModel WishPage { get; set; } = new();
    public ListPositionModel SubscriberPage { get; set; } = new();
    public ListPositionModel SubscriptionPage { get; set; } = new();
    public ListPositionModel ClaimPage { get; set; } = new();
    public ListPositionModel AdminBroadcastPage { get; set; } = new();
    public ListPositionModel AdminUserPage { get; set; } = new();

    public ListPositionModel GetListPosition(ListPosition position)
    {
        return position switch
        {
            ListPosition.Wish => WishPage,
            ListPosition.Subscriber => SubscriberPage,
            ListPosition.Subscription => SubscriptionPage,
            ListPosition.Claim => ClaimPage,
            ListPosition.AdminBroadcast => AdminBroadcastPage,
            ListPosition.AdminUserPage => AdminUserPage,
            _ => throw new NotSupportedException()
        };
    }
}
