using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class WishDraftModel
{
    [Key] public int WishDraftId { get; set; }
    public int? OriginalId { get; set; }
    public int OwnerId { get; set; }
    public int? ClaimerId { get; set; }

    [ForeignKey(nameof(OriginalId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public WishModel Original { get; set; }

    [ForeignKey(nameof(OwnerId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Owner { get; set; }

    [ForeignKey(nameof(ClaimerId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public UserModel Claimer { get; set; }

    [Required] public string Name { get; set; }
    public string Description { get; set; }
    public string FileId { get; set; }
    public Price PriceRange { get; set; }

    [InverseProperty(nameof(LinkModel.WishDraft))]
    public List<LinkModel> Links { get; } = new();

    public static WishDraftModel FromWish(WishModel wish)
    {
        var draft = new WishDraftModel()
        {
            ClaimerId = wish.ClaimerId,
            OwnerId = wish.OwnerId,
            Name = wish.Name,
            Description = wish.Description,
            FileId = wish.FileId,
            PriceRange = wish.PriceRange,
            Original = wish,
        };

        foreach (var draftLink in wish.Links)
            draft.Links.Add(new LinkModel() { Url = draftLink.Url });

        return draft;
    }
}

