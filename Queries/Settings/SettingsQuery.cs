namespace WishlistBot.Queries.Settings;

public class SettingsQuery : IQuery
{
   private const string settingsEmoji = "\u2699\ufe0f";
   public string Caption => $"{settingsEmoji} Настройки";
   public string Data => "@settings";
}
