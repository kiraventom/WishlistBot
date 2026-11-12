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

    /// <summary>Ключ картинки в медиа-хранилище (см. <see cref="MediaItemModel"/>).</summary>
    public string MediaKey { get; set; }
    public Price PriceRange { get; set; }

    /// <summary>Черновик помнит, постоянный ли виш: тип переключается в окне редактирования.</summary>
    public bool IsPermanent { get; set; }

    /// <summary>Бронь, которую видно пользователям: у постоянного виша бронь скрыта.</summary>
    [NotMapped]
    public bool HasVisibleClaim => !IsPermanent && ClaimerId is not null;

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
            MediaKey = wish.MediaKey,
            PriceRange = wish.PriceRange,
            IsPermanent = wish.IsPermanent,
            Original = wish,
        };

        foreach (var draftLink in wish.Links)
            draft.Links.Add(new LinkModel() { Url = draftLink.Url });

        return draft;
    }
}

