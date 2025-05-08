using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WishlistBot.Model.Factory;

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

