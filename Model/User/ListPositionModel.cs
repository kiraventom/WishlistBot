using System.ComponentModel.DataAnnotations;

namespace WishlistBot.Model.User;

public class ListPositionModel : IListPage
{
    [Key]
    public int ListPositionId { get; set; }

    public int Page { get; set; }
}

