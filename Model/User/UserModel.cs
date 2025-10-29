using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

[Index(nameof(TelegramId), IsUnique=true)]
public class UserModel
{
    [Key] public int UserId { get; set; }

    public long TelegramId { get; set; }
    [Required] public string FirstName { get; set; }
    [Required] public string SubscribeId { get; set; }
    public string Tag { get; set; }

    public bool IsAdmin { get; set; }

    public BotState BotState { get; set; }
    public string LastQueryId { get; set; }
    public int? LastBotMessageId { get; set; }
    public string QueryParams { get; set; }
    public string AllowedQueries { get; set; }

    [InverseProperty(nameof(WishDraftModel.Owner))]
    public WishDraftModel CurrentWish { get; set; }

    [Required] public SettingsModel Settings { get; set; }
    [Required] public ProfileModel Profile { get; set; }

    [InverseProperty(nameof(WishModel.Owner))]
    public List<WishModel> Wishes { get; } = new();

    [InverseProperty(nameof(SubscriptionModel.Subscriber))]
    public List<SubscriptionModel> Subscriptions { get; } = new();

    [InverseProperty(nameof(SubscriptionModel.Target))]
    public List<SubscriptionModel> Subscribers { get; } = new();

    [InverseProperty(nameof(WishModel.Claimer))]
    public List<WishModel> ClaimedWishes { get; } = new();

    public List<ReceivedBroadcastModel> ReceivedBroadcasts { get; } = new();

    [InverseProperty(nameof(WishViewSettingsModel.Viewer))]
    public List<WishViewSettingsModel> WishViewSettings { get; } = new();

    public IOrderedEnumerable<WishModel> GetSortedWishes(WishViewSettingsModel viewSettings)
    {
        var descending = viewSettings?.Descending ?? false;
        var property = viewSettings?.SortProperty ?? SortProperty.Default;
        var onlyUnclaimed = viewSettings?.OnlyUnclaimed ?? false;

        return property switch
        {
            SortProperty.Default when descending && onlyUnclaimed => Wishes.Where(w => w.ClaimerId == null).OrderByDescending(w => w.Order),
            SortProperty.Price when descending && onlyUnclaimed => Wishes.Where(w => w.ClaimerId == null).OrderByDescending(w => ((int)w.PriceRange)),
            SortProperty.Default when descending => Wishes.OrderByDescending(w => w.Order),
            SortProperty.Price when descending => Wishes.OrderByDescending(w => ((int)w.PriceRange)),
            SortProperty.Default when onlyUnclaimed => Wishes.Where(w => w.ClaimerId == null).OrderBy(w => w.Order),
            SortProperty.Price when onlyUnclaimed => Wishes.Where(w => w.ClaimerId == null).OrderBy(w => ((int)w.PriceRange)),
            SortProperty.Default => Wishes.OrderBy(w => w.Order),
            SortProperty.Price => Wishes.OrderBy(w => ((int)w.PriceRange)),
            _ => Wishes.OrderByDescending(w => w.Order)
        };
    }

    public List<WishModel> GetSortedClaimedWishes()
    {
        ClaimedWishes.Sort((w0, w1) => w0.Order.CompareTo(w1.Order));
        return ClaimedWishes;
    }

    public string GetSubscribeLink() => $"https://t.me/{Config.Instance.Username}?start={SubscribeId}";
    
    public WishViewSettingsModel GetOrCreateWishViewSettings(int targetId)
    {
        var target = WishViewSettings.FirstOrDefault(wvs => wvs.TargetId == targetId);
        if (target is null)
        {
            target = new WishViewSettingsModel()
            {
                TargetId = targetId,
                ViewerId = UserId,
            };

            WishViewSettings.Add(target);
        }

        return target;
    }
}

