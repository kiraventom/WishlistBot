using Microsoft.EntityFrameworkCore;
using WishlistBot.Media;

namespace WishlistBot.Model.User;

public class UserContext : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<WishModel> Wishes { get; set; }
    public DbSet<WishDraftModel> WishDrafts { get; set; }
    public DbSet<SettingsModel> Settings { get; set; }
    public DbSet<LinkModel> Links { get; set; }
    public DbSet<FriendModel> Friends { get; set; }
    public DbSet<BroadcastModel> Broadcasts { get; set; }
    public DbSet<ReceivedBroadcastModel> ReceivedBroadcasts { get; set; }
    public DbSet<NotificationModel> Notifications { get; set; }
    public DbSet<PendingNotificationModel> PendingNotifications { get; set; }
    public DbSet<UserExtraModel> UserExtra { get; set; }
    public DbSet<ScheduledNotificationModel> ScheduledNotifications { get; set; }
    public DbSet<WishlistCleanupStateModel> WishlistCleanupStates { get; set; }
    public DbSet<WishlistFillStateModel> WishlistFillStates { get; set; }
    public DbSet<WishEmojiPackModel> EmojiPacks { get; set; }
    public DbSet<WishEmojiAvatarModel> Avatars { get; set; }

    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {
    }

    /// <summary>
    /// Удаление виша или выброс черновика освобождает ключ картинки. Собираем такие ключи
    /// здесь и после коммита апдейта проверяем, не ссылается ли на них кто-то ещё:
    /// внутри транзакции удалённые строки ещё видны, и проверка соврала бы.
    /// </summary>
    public override int SaveChanges()
    {
        // Черновик, отвязанный от пользователя (CurrentWish = null), становится Deleted
        // только после DetectChanges — без него освобождённые ключи потеряются
        ChangeTracker.DetectChanges();

        var releasedMediaKeys = ChangeTracker.Entries<WishModel>()
            .Where(entry => entry.State == EntityState.Deleted)
            .Select(entry => entry.Entity.MediaKey)
            .Concat(ChangeTracker.Entries<WishDraftModel>()
                .Where(entry => entry.State == EntityState.Deleted)
                .Select(entry => entry.Entity.MediaKey));

        MediaStorage.ReleaseLater(releasedMediaKeys);

        return base.SaveChanges();
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

        // Все строки-спутники пользователя нужно загрузить: иначе проверки «is null»
        // ниже создают их заново на каждом апдейте и EF пытается вставить дубликаты
        // (PK у них — UserId, поэтому второй INSERT падает на UNIQUE constraint).
        var userModel = this.Users
            .Include(u => u.Settings)
            .Include(u => u.Profile)
            .Include(u => u.ListPositions)
            .Include(u => u.WishlistCleanupState)
            .Include(u => u.WishlistFillState)
            .Include(u => u.EmojiPack)
            .Include(u => u.Avatar)
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
            userModel.Settings = new SettingsModel()
            {
                ReceiveFriendWishNotifications = true,
                SendNotifications = true,
                BirthdayNotifications = true,
                WishlistCleanupNotifications = true,
                WishlistFillNotifications = true
            };

        if (userModel.Profile is null)
            userModel.Profile = new ProfileModel();

        if (userModel.ListPositions is null)
            userModel.ListPositions = new ListPositionsModel();

        if (userModel.WishlistCleanupState is null)
            userModel.WishlistCleanupState = new WishlistCleanupStateModel();

        if (userModel.WishlistFillState is null)
            userModel.WishlistFillState = new WishlistFillStateModel();

        if (userModel.EmojiPack is null)
            userModel.EmojiPack = new WishEmojiPackModel() { CreatedAt = DateTime.UtcNow };

        if (userModel.Avatar is null)
            userModel.Avatar = new WishEmojiAvatarModel();

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
        modelBuilder.Entity<PendingNotificationModel>().Property(e => e.Type).HasConversion<int>();
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

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.ListPositions)
            .WithOne(e => e.User)
            .HasForeignKey<ListPositionsModel>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.WishlistCleanupState)
            .WithOne(e => e.User)
            .HasForeignKey<WishlistCleanupStateModel>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PendingNotificationModel>()
            .HasOne(e => e.Recepient)
            .WithMany(e => e.PendingNotifications)
            .HasForeignKey(e => e.RecepientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PendingNotificationModel>()
            .HasOne(e => e.Notification)
            .WithMany()
            .HasForeignKey(e => e.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.WishlistFillState)
            .WithOne(e => e.User)
            .HasForeignKey<WishlistFillStateModel>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.EmojiPack)
            .WithOne(e => e.User)
            .HasForeignKey<WishEmojiPackModel>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserModel>()
            .HasOne(e => e.Avatar)
            .WithOne(e => e.User)
            .HasForeignKey<WishEmojiAvatarModel>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
