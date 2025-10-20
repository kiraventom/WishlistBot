using Serilog;
using Telegram.Bot;
using WishlistBot.Model;

namespace WishlistBot.Extra;

public class KeyboardCleaner
{
    private readonly HashSet<int> _current = [];

    public static KeyboardCleaner Instance { get; } = new();

    private KeyboardCleaner() { }

    public void CleanAll(ILogger logger, ITelegramBotClient client, UserContext userContext, UserModel user)
    {
        if (user.LastBotMessageId is null)
            return;

        var userExtra = userContext.UserExtra.FirstOrDefault(e => e.UserId == user.UserId);
        if (userExtra is null)
        {
            userExtra = new UserExtraModel() { UserId = user.UserId };
            userContext.UserExtra.Add(userExtra);
            userContext.SaveChanges();
        }

        if (userExtra.KeyboardCleaned || _current.Contains(user.UserId))
            return;

        _current.Add(user.UserId);

        Task.Run(async () =>
        {
            for (int messageId = (userExtra.LastCleanedMessageId ?? user.LastBotMessageId.Value) - 1; messageId >= -1; --messageId)
            {
                var localUserContext = UserContext.Create();
                await client.ClearKeyboard(logger, user.TelegramId, messageId, suppressLog: true);

                var localUserExtra = localUserContext.UserExtra.First(e => e.UserId == user.UserId);
                localUserExtra.LastCleanedMessageId = messageId;
                localUserContext.SaveChanges();
                await Task.Delay(1000);
            }

            _current.Remove(user.UserId);
        });
    }
}

