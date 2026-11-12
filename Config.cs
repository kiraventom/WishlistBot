using System.Text.Json;
using System.Text.Json.Serialization;

namespace WishlistBot;

[method: JsonConstructor]
public class Config(string token, string username, string userConnectionString, string mediaStorageConnectionString, long storageChannelId)
{
    public static Config Instance { get; private set; }

    /// <summary>
    /// Telegram bot token. Received from <a href="https://t.me/BotFather">BotFather</a>
    /// </summary>
    public string Token { get; } = token;
    ///
    /// <summary>
    /// Telegram username
    /// </summary>
    public string Username { get; } = username;

    /// <summary>
    /// SQlite connection string to User DB
    /// </summary>
    public string UserConnectionString { get; } = userConnectionString;

    /// <summary>
    /// SQlite connection string to Media Storage DB
    /// </summary>
    public string MediaStorageConnectionString { get; } = mediaStorageConnectionString;

    /// <summary>
    /// ID of the channel that used to work as media storage: pictures lived there and only
    /// their telegram file_id was kept in the database.
    ///
    /// Pictures are now stored as blobs in the media database, so the channel is only needed
    /// to delete the old messages after they have been imported. Can be left as is.
    /// </summary>
    public long StorageChannelId { get; } = storageChannelId;

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
