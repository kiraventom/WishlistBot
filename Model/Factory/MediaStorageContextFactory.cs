using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.Factory;

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

