namespace WishlistBot.Queries.Notification;

public class WishlistCleanupSnoozeQuery : IQuery
{
    private const string clockEmoji = "\u23f0";

    public string Caption => $"{clockEmoji} Напомнить завтра";
    public string Data => "@cleanup_snooze";
}
