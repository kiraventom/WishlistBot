using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class ReceivedBroadcastModel
{
    [Key] public int ReceivedBroadcastId { get; set; }
    public int ReceiverId { get; set; }
    public int BroadcastId { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Receiver { get; set; }

    [ForeignKey(nameof(BroadcastId))]
    public BroadcastModel Broadcast { get; set; }
    public int MessageId { get; set; }
}

