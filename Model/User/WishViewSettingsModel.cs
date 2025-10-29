using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class WishViewSettingsModel
{
    [Key] public int WishViewSettingsId { get; set; }
    public int TargetId { get; set; }
    public int ViewerId { get; set; }

    [ForeignKey(nameof(TargetId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Target { get; set; }

    [ForeignKey(nameof(ViewerId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Viewer { get; set; }

    public bool Descending { get; set; }
    public SortProperty SortProperty { get; set; }

    public bool OnlyUnclaimed { get; set; }
}

