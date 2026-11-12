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

    /// <summary>Ключ картинки в медиа-хранилище (см. <see cref="MediaItemModel"/>).</summary>
    public string MediaKey { get; set; }
    public Price PriceRange { get; set; }

    /// <summary>
    /// Постоянный виш — подарок, которому владелец рад всегда: его нельзя забронировать,
    /// а старая бронь (если была) остаётся в базе, но нигде не показывается.
    /// </summary>
    public bool IsPermanent { get; set; }

    /// <summary>Бронь, которую видно пользователям: у постоянного виша бронь скрыта.</summary>
    [NotMapped]
    public bool HasVisibleClaim => !IsPermanent && ClaimerId is not null;

    /// <summary>
    /// Кому уходит уведомление об изменении или удалении виша: только тому, кто его
    /// забронировал. У постоянного виша бронь скрыта, поэтому не уведомляем никого.
    /// </summary>
    [NotMapped]
    public int? ClaimNotificationRecipientId => IsPermanent ? null : ClaimerId;

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
            MediaKey = draft.MediaKey,
            PriceRange = draft.PriceRange,
            IsPermanent = draft.IsPermanent,
        };

        if (draft.Original is not null)
            wish.Order = draft.Original.Order;

        foreach (var draftLink in draft.Links)
            wish.Links.Add(new LinkModel() { Url = draftLink.Url });

        return wish;
    }

    public string BuildLink(string subscribeId, bool returnToClaims = false) =>
        DeepLink.BuildWish(subscribeId[..20], WishId, returnToClaims);
}

