using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

public class WishModel
{
    [Key] public int WishId { get; set; }
    public int OwnerId { get; set; }
    public int? ClaimerId { get; set; }

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

    [InverseProperty(nameof(LinkModel.Wish))]
    public List<LinkModel> Links { get; } = new();
    [Required] public int Order { get; set; }

    public static WishModel FromDraft(WishDraftModel draft)
    {
        var wish = new WishModel()
        {
            ClaimerId = draft.ClaimerId,
            OwnerId = draft.OwnerId,
            Name = draft.Name,
            Description = draft.Description,
            FileId = draft.FileId,
            PriceRange = draft.PriceRange,
        };

        if (draft.Original is not null)
            wish.Order = draft.Original.Order;

        foreach (var draftLink in draft.Links)
            wish.Links.Add(new LinkModel() { Url = draftLink.Url });

        return wish;
    }

    public string BuildLink(string subscribeId)
    {
        subscribeId = subscribeId[..20];
        return $"t.me/{Config.Instance.Username}?start=a=sw_u={subscribeId}_w={WishId}"; }
}

