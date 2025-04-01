using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model;

public class MediaStorageContext : DbContext
{
    public DbSet<MediaItemModel> StoredMedia { get; set; }

    public MediaStorageContext(DbContextOptions<MediaStorageContext> options) : base(options)
    {
    }

    public static MediaStorageContext Create()
    {
        var builder = new DbContextOptionsBuilder<MediaStorageContext>();
        builder.UseSqlite(Config.Instance.MediaStorageConnectionString);

        return new MediaStorageContext(builder.Options);
    }
}

public class MediaItemModel
{
    [Key] public int MediaItemId { get; set; }

    [Required] public string FileId { get; set; }
    public int MessageId { get; set; }
}
