using System.Text.Json;
using System.Text.Json.Serialization;

namespace WishlistBot;

[method: JsonConstructor]
public class Config(string token, string userConnectionString, string mediaStorageConnectionString, long storageChannelId, long adminId, string telesinkChatId)
{
    public static Config Instance { get; private set; }

    /// <summary>
    /// Telegram bot token. Received from <a href="https://t.me/BotFather">BotFather</a>
    /// </summary>
    public string Token { get; } = token;

    /// <summary>
    /// SQlite connection string to User DB
    /// </summary>
    public string UserConnectionString { get; } = userConnectionString;

    /// <summary>
    /// SQlite connection string to Media Storage DB
    /// </summary>
    public string MediaStorageConnectionString { get; } = mediaStorageConnectionString;

    /// <summary>
    /// ID of a channel that will work as media storage. 
    /// Bot will copy all received media to this channel to cache them.
    /// It is required because otherwise Telegram will delete them from servers if user deletes them from dialog.
    /// </summary>
    public long StorageChannelId { get; } = storageChannelId;

    /// <summary>
    /// Only user with this ID will have access to admin commands.
    /// </summary>
    public long AdminId { get; } = adminId;

    /// <summary>
    /// Chat ID where Telesink logs will get sent. If no ID is provided,
    /// Telesink will not be enabled;
    /// </summary>
    public string TelesinkChatId { get; } = telesinkChatId;

    public static Config Load(string filepath)
    {
        if (Instance is not null)
            return Instance;

        try
        {
            using var configFile = File.OpenRead(filepath);
            Instance = JsonSerializer.Deserialize<Config>(configFile, CommonOptions.Json);
        }
        catch (Exception)
        {
        }

        return Instance;
    }
}
