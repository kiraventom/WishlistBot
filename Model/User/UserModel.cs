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

    /// <summary>
    /// Когда пользователь последний раз что-то прислал боту (сообщение или нажатие кнопки),
    /// в UTC. Пока с этого момента не прошло <see cref="WishlistBot.Notification.NotificationDispatchService.IdleMinutes"/>
    /// минут, уведомления копятся в очереди и не отвлекают от того, что человек делает.
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// С прошлого экрана пользователю приходило уведомление. Тогда следующий экран
    /// отправляем новым сообщением: в ленте «All» он окажется под уведомлением,
    /// а не над ним (старый экран остаётся выше и его не видно).
    /// </summary>
    public bool NotifiedSinceLastMessage { get; set; }

    public int? LastBotMessageId { get; set; }
    public string QueryParams { get; set; }
    public string AllowedQueries { get; set; }

    [InverseProperty(nameof(WishDraftModel.Owner))]
    public WishDraftModel CurrentWish { get; set; }

    [Required] public SettingsModel Settings { get; set; }
    [Required] public ProfileModel Profile { get; set; }
    [Required] public ListPositionsModel ListPositions { get; set; }

    public WishlistCleanupStateModel WishlistCleanupState { get; set; }

    public WishlistFillStateModel WishlistFillState { get; set; }

    [InverseProperty(nameof(WishModel.Owner))]
    public List<WishModel> Wishes { get; } = new();

    [InverseProperty(nameof(FriendModel.User))]
    public List<FriendModel> Friends { get; } = new();

    [InverseProperty(nameof(WishModel.Claimer))]
    public List<WishModel> ClaimedWishes { get; } = new();

    public List<ReceivedBroadcastModel> ReceivedBroadcasts { get; } = new();

    [InverseProperty(nameof(PendingNotificationModel.Recepient))]
    public List<PendingNotificationModel> PendingNotifications { get; } = new();

    [InverseProperty(nameof(WishViewSettingsModel.Viewer))]
    public List<WishViewSettingsModel> WishViewSettings { get; } = new();
}

