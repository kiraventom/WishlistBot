using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using WishlistBot.Notification;
using WishlistBot.QueryParameters;

namespace WishlistBot.Model;

public class UserContext : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<WishModel> Wishes { get; set; }
    public DbSet<WishDraftModel> WishDrafts { get; set; }
    public DbSet<SettingsModel> Settings { get; set; }
    public DbSet<LinkModel> Links { get; set; }
    public DbSet<SubscriptionModel> Subscriptions { get; set; }
    public DbSet<BroadcastModel> Broadcasts { get; set; }
    public DbSet<ReceivedBroadcastModel> ReceivedBroadcasts { get; set; }
    public DbSet<NotificationModel> Notifications { get; set; }

    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {
    }

    public static UserContext Create()
    {
        var builder = new DbContextOptionsBuilder<UserContext>();
        builder.UseSqlite(Config.Instance.UserConnectionString);

        return new UserContext(builder.Options);
    }

    public UserModel GetOrAddUser(long telegramId, string firstName, string username)
    {
        var isNew = false;

        var userModel = this.Users
            .Include(u => u.Settings)
            .Include(u => u.Profile)
            .FirstOrDefault(u => u.TelegramId == telegramId);

        if (userModel is null)
        {
            isNew = true;
            userModel = new UserModel()
            {
                FirstName = firstName,
                Tag = username,
                TelegramId = telegramId,
                SubscribeId = Guid.NewGuid().ToString("N")
            };
        }

        if (userModel.FirstName != firstName)
            userModel.FirstName = firstName;

        if (userModel.Tag != username)
            userModel.Tag = username;

        if (userModel.Settings is null)
            userModel.Settings = new SettingsModel() { ReceiveNotifications = true, SendNotifications = true };

        if (userModel.Profile is null)
            userModel.Profile = new ProfileModel();

        if (isNew)
        {
            Users.Add(userModel);
            this.SaveChanges();
        }

        return userModel;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies(false);
        optionsBuilder.AddInterceptors(new OrderAssignmentInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>().Property(e => e.BotState).HasConversion<int>();
        modelBuilder.Entity<WishModel>().Property(e => e.PriceRange).HasConversion<int>();
        modelBuilder.Entity<WishDraftModel>().Property(e => e.PriceRange).HasConversion<int>();
        modelBuilder.Entity<NotificationModel>().Property(e => e.Type).HasConversion<int>();

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.Settings)
            .WithOne(e => e.User)
            .HasForeignKey<SettingsModel>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.Profile)
            .WithOne(e => e.User)
            .HasForeignKey<ProfileModel>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

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

    public List<WishModel> GetSortedWishes()
    {
        Wishes.Sort((w0, w1) => w0.Order.CompareTo(w1.Order));
        return Wishes;
    }
}

public class ProfileModel
{
    [Key] public int ProfileId { get; set; }

    public int UserId { get; set; }
    public UserModel User { get; set; }

    public DateOnly? Birthday { get; set; }
    public string Notes { get; set; }
}

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
}

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

public class SettingsModel
{
    [Key] public int SettingsId { get; set; }

    public int UserId { get; set; }
    public UserModel User { get; set; }

    public bool SendNotifications { get; set; }
    public bool ReceiveNotifications { get; set; }

    public void SetFromEnum(SettingsEnum settingsEnum)
    {
        SendNotifications = settingsEnum.HasFlag(SettingsEnum.SendNotifications);
        ReceiveNotifications = settingsEnum.HasFlag(SettingsEnum.ReceiveNotifications);
    }

    public SettingsEnum ToEnum()
    {
        var settingsEnum = SettingsEnum.None;

        if (SendNotifications)
            settingsEnum |= SettingsEnum.SendNotifications;

        if (ReceiveNotifications)
            settingsEnum |= SettingsEnum.ReceiveNotifications;

        return settingsEnum;
    }
}

[Index(nameof(SubscriberId), nameof(TargetId), IsUnique=true)]
public class SubscriptionModel
{
    [Key] public int SubscriptionId { get; set; }
    public int SubscriberId { get; set; }
    public int TargetId { get; set; }

    [ForeignKey(nameof(SubscriberId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Subscriber { get; set; }

    [ForeignKey(nameof(TargetId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Target { get; set; }
}

public class BroadcastModel
{
    [Key] public int BroadcastId { get; set; }

    [Required] public string Text { get; set; }
    public string FileId { get; set; }
    public DateTime? DateTimeSent { get; set; }
    public bool Deleted { get; set; }

    public string GetShortText()
    {
        if (Text is null)
            return "<empty>";

        return Text.Length <= 20
           ? Text
           : Text[..19] + "…";
    }
}

public class ReceivedBroadcastModel
{
    [Key] public int ReceivedBroadcastId { get; set; }
    public int ReceiverId { get; set; }
    public int BroadcastId { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel Receiver { get; set; }

    [ForeignKey(nameof(BroadcastId))]
    public BroadcastModel Broadcast { get; set; }
    public int MessageId { get; set; }
}

public class NotificationModel
{
    [Key] public int NotificationId { get; set; }
    public int SourceId { get; set; }

    public NotificationMessageType Type { get; set; }
    public int? SubjectId { get; set; }
    public string Extra { get; set; }

    [ForeignKey(nameof(SourceId))]
    public UserModel Source { get; set; }

    public bool GetExtraBool()
    {
        if (Extra is null)
            return false;

        var byteArray = Convert.FromBase64String(Extra);
        return BitConverter.ToBoolean(byteArray);
    }

    public void SetExtraBool(bool value)
    {
        var byteArray = BitConverter.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }

    public int GetExtraInt()
    {
        if (Extra is null)
            return -1;

        var byteArray = Convert.FromBase64String(Extra);
        return BitConverter.ToInt32(byteArray);
    }

    public void SetExtraInt(int value)
    {
        var byteArray = BitConverter.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }

    public double GetExtraDouble()
    {
        if (Extra is null)
            return double.NaN;

        var byteArray = Convert.FromBase64String(Extra);
        return BitConverter.ToDouble(byteArray);
    }

    public void SetExtraDouble(double value)
    {
        var byteArray = BitConverter.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }

    public string GetExtraString()
    {
        if (Extra is null)
            return null;

        var byteArray = Convert.FromBase64String(Extra);
        return Encoding.UTF8.GetString(byteArray);
    }

    public void SetExtraString(string value)
    {
        var byteArray = Encoding.UTF8.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }
}
    
public class OrderAssignmentInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null)
            return result;

        var addedEntries = eventData.Context.ChangeTracker.Entries<WishModel>().Where(e => e.State == EntityState.Added);
        var wishes = eventData.Context.Set<WishModel>().Local;
        foreach (var entry in addedEntries)
        {
            if (entry.Entity.Order > 0)
                continue;

            var sameUserWishes = wishes.Where(w => w.OwnerId == entry.Entity.OwnerId);

            if (wishes.Any())
                entry.Entity.Order = sameUserWishes.Max(w => w.Order) + 1;
            else
                entry.Entity.Order = 0;
        }

        return result;
    }
}
