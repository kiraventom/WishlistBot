using Serilog;
using Telegram.Bot;
using WishlistBot.Model;

namespace WishlistBot.Jobs;

public delegate Task NotificationJobActionDelegate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId, int notificationId);
public delegate Task BroadcastJobActionDelegate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId, int broadcastId);
