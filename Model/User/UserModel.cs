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
    [Required] public ListPositionsModel ListPositions { get; set; }

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
}

