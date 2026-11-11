using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

// Настройки просмотра чужого вишлиста: своя открытая страница, сортировка и фильтр
// на каждую пару «зритель → чей вишлист».
public class WishViewSettingsModel : IViewerTarget, IListPage
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

    public int Page { get; set; }
}

