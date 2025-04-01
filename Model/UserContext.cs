using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using WishlistBot.Database.Users;

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
        var userModel = this.Users.FirstOrDefault(u => u.TelegramId == telegramId);
        if (userModel is null)
        {
            userModel = new UserModel()
            {
                FirstName = firstName,
                Tag = username
            };
        }

        if (userModel.FirstName != firstName)
            userModel.FirstName = firstName;

        if (userModel.Tag != username)
            userModel.Tag = username;

        return userModel;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies(false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>()
            .HasMany(e => e.Subscriptions)
            .WithOne(e => e.Subscriber);

        modelBuilder.Entity<UserModel>()
            .HasMany(e => e.Subscribers)
            .WithOne(e => e.Target);

        modelBuilder.Entity<UserModel>()
            .Property(e => e.BotState)
            .HasConversion<int>();

        modelBuilder.Entity<WishModel>()
            .Property(e => e.PriceRange)
            .HasConversion<int>();

        modelBuilder.Entity<WishDraftModel>()
            .Property(e => e.PriceRange)
            .HasConversion<int>();

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.Settings)
            .WithOne(e => e.User)
            .HasForeignKey<SettingsModel>(e => e.UserId);

        modelBuilder.Entity<UserModel>()
            .HasIndex(e => e.TelegramId)
            .IsUnique();

        modelBuilder.Entity<WishModel>()
            .HasOne(e => e.Owner)
            .WithMany(e => e.Wishes);

        modelBuilder.Entity<WishModel>()
            .HasOne(e => e.Claimer)
            .WithMany(e => e.ClaimedWishes);

        modelBuilder.Entity<WishDraftModel>()
            .HasOne(e => e.Owner)
            .WithOne(e => e.CurrentWish)
            .HasForeignKey<WishDraftModel>(e => e.OwnerId);

        modelBuilder.Entity<SubscriptionModel>()
            .HasIndex(e => new { e.SubscriberId, e.TargetId })
            .IsUnique();
    }
}

public class UserModel
{
    [Key] public int UserId { get; set; }

    public long TelegramId { get; set; }
    [Required] public string FirstName { get; set; }
    [Required] public string SubscribeId { get; set; }
    public string Tag { get; set; }

    public BotState BotState { get; set; }
    public string LastQueryId { get; set; }
    public int? LastBotMessageId { get; set; }
    public string QueryParams { get; set; }
    public string AllowedQueries { get; set; }

    public WishDraftModel CurrentWish { get; set; }
    [Required] public SettingsModel Settings { get; set; }
    public List<WishModel> Wishes { get; } = new();
    public List<SubscriptionModel> Subscriptions { get; } = new();
    public List<SubscriptionModel> Subscribers { get; } = new();
    public List<WishModel> ClaimedWishes { get; } = new();
    public List<ReceivedBroadcastModel> ReceivedBroadcasts { get; } = new();
}

public class WishModel
{
    [Key] public int WishId { get; set; }
    public int OwnerId { get; set; }
    public int? ClaimerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public UserModel Owner { get; set; }

    [ForeignKey(nameof(ClaimerId))]
    public UserModel Claimer { get; set; }

    [Required] public string Name { get; set; }
    public string Description { get; set; }
    public string FileId { get; set; }
    public Price PriceRange { get; set; }
    public List<LinkModel> Links { get; } = new();
}

public class WishDraftModel
{
    [Key] public int WishDraftId { get; set; }
    public int? OriginalId { get; set; }
    public int OwnerId { get; set; }
    public int? ClaimerId { get; set; }

    [ForeignKey(nameof(OriginalId))]
    public WishModel Original { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public UserModel Owner { get; set; }

    [ForeignKey(nameof(ClaimerId))]
    public UserModel Claimer { get; set; }

    [Required] public string Name { get; set; }
    public string Description { get; set; }
    public string FileId { get; set; }
    public Price PriceRange { get; set; }
    public List<LinkModel> Links { get; } = new();
}

public class LinkModel
{
    [Key] public int LinkId { get; set; }
    public int WishId { get; set; }

    [ForeignKey(nameof(WishId))]
    public WishModel Wish { get; set; }
    [Required] public string Url { get; set; }
}

public class SettingsModel
{
    [Key] public int SettingsId { get; set; }
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; }

    public bool SendNotifications { get; set; }
    public bool ReceiveNotifications { get; set; }
}

public class SubscriptionModel
{
    [Key] public int SubscriptionId { get; set; }
    public int SubscriberId { get; set; }
    public int TargetId { get; set; }

    [ForeignKey(nameof(SubscriberId))]
    public UserModel Subscriber { get; set; }

    [ForeignKey(nameof(TargetId))]
    public UserModel Target { get; set; }
}

public class BroadcastModel
{
    [Key] public int BroadcastId { get; set; }

    [Required] public string Text { get; set; }
    public string FileId { get; set; }
    public DateTime? DateTimeSent { get; set; }
    public bool Deleted { get; set; }
}

public class ReceivedBroadcastModel
{
    [Key] public int ReceivedBroadcastId { get; set; }
    public int ReceiverId { get; set; }
    public int BroadcastId { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    public UserModel Receiver { get; set; }

    [ForeignKey(nameof(BroadcastId))]
    public BroadcastModel Broadcast { get; set; }
    public int MessageId { get; set; }
}
