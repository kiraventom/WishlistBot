using Serilog;
using Telegram.Bot;
using WishlistBot.Model.User;

namespace WishlistBot.Jobs;

public delegate Task BroadcastJobActionDelegate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId, int broadcastId);
