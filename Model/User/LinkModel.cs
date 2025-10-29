using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class LinkModel
{
    [Key] public int LinkId { get; set; }

    public int? WishId { get; set; }
    public int? WishDraftId { get; set; }

    [ForeignKey(nameof(WishId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public WishModel Wish { get; set; }

    [ForeignKey(nameof(WishDraftId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public WishDraftModel WishDraft { get; set; }

    [Required] public string Url { get; set; }

    public override int GetHashCode() => Url.GetHashCode();

    public override bool Equals(object obj) => obj is LinkModel linkModel && string.Equals(Url, linkModel.Url, StringComparison.OrdinalIgnoreCase);
}

