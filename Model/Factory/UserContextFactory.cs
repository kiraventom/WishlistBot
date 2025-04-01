using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.Factory;

public class UserDesignTimeContextFactory : DesignTimeContextFactory<UserContext>
{
    public override UserContext CreateDbContext(string[] args)
    {
        var config = LoadConfig(args);
        var builder = new DbContextOptionsBuilder<UserContext>();
        builder.UseSqlite(config.UserConnectionString);

        return new UserContext(builder.Options);
    }
}

