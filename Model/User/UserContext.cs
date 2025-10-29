using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

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
    public DbSet<UserExtraModel> UserExtra { get; set; }

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
        modelBuilder.Entity<WishViewSettingsModel>().Property(e => e.SortProperty).HasConversion<int>();

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
