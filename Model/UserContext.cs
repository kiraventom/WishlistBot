using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WishlistBot.Database.Users;

namespace WishlistBot.Model;

public abstract class DesignTimeContextFactory<T> : IDesignTimeDbContextFactory<T> where T : DbContext
{
    public abstract T CreateDbContext(string[] args);

    protected Config LoadConfig(string[] args)
    {
        if (args.Length < 1)
            throw new NotSupportedException("Arg 0 should contain path to config.json");

        var configFilePath = args[0];
        var config = Config.Load(configFilePath);
        if (config is null)
            throw new NotSupportedException("config.json is invalid");

        return config;
    }
}

public class UserContextFactory : DesignTimeContextFactory<UserContext>
{
    public override UserContext CreateDbContext(string[] args)
    {
        var config = LoadConfig(args);
        var builder = new DbContextOptionsBuilder<UserContext>();
        builder.UseSqlite(config.UserConnectionString);

        return new UserContext(builder.Options);
    }
}

public class MediaStorageContextFactory : DesignTimeContextFactory<MediaStorageContext>
{
    public override MediaStorageContext CreateDbContext(string[] args)
    {
        var config = LoadConfig(args);
        var builder = new DbContextOptionsBuilder<MediaStorageContext>();
        builder.UseSqlite(config.MediaStorageConnectionString);

        return new MediaStorageContext(builder.Options);
    }
}

public class UserContext : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<WishModel> Wishes { get; set; }
    public DbSet<WishDraftModel> WishDrafts { get; set; }
    public DbSet<SettingsModel> Settings { get; set; }
    public DbSet<SubscriptionModel> Subscriptions { get; set; }
    public DbSet<BroadcastModel> Broadcasts { get; set; }
    public DbSet<ReceivedBroadcastModel> ReceivedBroadcasts { get; set; }

    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {
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
            .HasOne(e => e.Settings)
            .WithOne(e => e.User)
            .HasForeignKey<SettingsModel>(e => e.UserId);

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

        modelBuilder.Entity<BroadcastModel>()
            .Ignore(e => e.IsSent);
    }
}

public class MediaStorageContext : DbContext
{
    public DbSet<MediaItemModel> StoredMedia { get; set; }

    public MediaStorageContext(DbContextOptions<MediaStorageContext> options) : base(options)
    {
    }
}

[PrimaryKey(nameof(UserId))]
public class UserModel
{
    public int UserId { get; set; }
    public long TelegramId { get; set; }
    public string FirstName { get; set; }
    public string Tag { get; set; }

    public BotState BotState { get; set; }
    public string LastQueryId { get; set; }
    public int LastBotMessageId { get; set; }
    public string QueryParams { get; set; }
    public string AllowedQueries { get; set; }

    public List<WishModel> Wishes { get; } = new();

    public WishDraftModel CurrentWish { get; set; }
    public SettingsModel Settings { get; set; }

    public string SubscribeId { get; set; }
    public List<SubscriptionModel> Subscriptions { get; } = new();
    public List<SubscriptionModel> Subscribers { get; } = new();
    public List<WishModel> ClaimedWishes { get; } = new();

    public List<ReceivedBroadcastModel> ReceivedBroadcasts { get; } = new();
}

[PrimaryKey(nameof(WishId))]
public class WishModel
{
    public int WishId { get; set; }
    public UserModel Owner { get; set; }
    public UserModel Claimer { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public string FileId { get; set; }
    public Price PriceRange { get; set; }
    public List<LinkModel> Links { get; } = new();
}

[PrimaryKey(nameof(WishDraftId))]
public class WishDraftModel
{
    public int WishDraftId { get; set; }
    public WishModel Original { get; set; }

    public int OwnerId { get; set; }
    public UserModel Owner { get; set; }
    public UserModel Claimer { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public string FileId { get; set; }
    public Price PriceRange { get; set; }
    public List<LinkModel> Links { get; } = new();
}

[PrimaryKey(nameof(LinkId))]
public class LinkModel
{
    public int LinkId { get; set; }
    public WishModel Wish { get; set; }
    public string Url { get; set; }
}

[PrimaryKey(nameof(SettingsId))]
public class SettingsModel
{
    public int SettingsId { get; set; }
    public int UserId { get; set; }
    public UserModel User { get; set; }

    public bool SendNotifications { get; set; }
    public bool ReceiveNotifications { get; set; }
}

[PrimaryKey(nameof(SubscriptionId))]
public class SubscriptionModel
{
    public int SubscriptionId { get; set; }
    public UserModel Subscriber { get; set; }
    public UserModel Target { get; set; }
}

[PrimaryKey(nameof(BroadcastId))]
public class BroadcastModel
{
    public int BroadcastId { get; set; }
    public string Text { get; set; }
    public string FileId { get; set; }
    public DateTime DateTimeSent { get; set; }
    public bool Deleted { get; set; }
    public bool IsSent => DateTimeSent != DateTime.MinValue;
}

[PrimaryKey(nameof(ReceivedBroadcastId))]
public class ReceivedBroadcastModel
{
    public int ReceivedBroadcastId { get; set; }
    public UserModel Receiver { get; set; }
    public BroadcastModel Broadcast { get; set; }
    public int MessageId { get; set; }
}

[PrimaryKey(nameof(MediaItemId))]
public class MediaItemModel
{
    public int MediaItemId { get; set; }
    public string FileId { get; set; }
    public int MessageId { get; set; }
}
