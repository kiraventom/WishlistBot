using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;
using WishlistBot.Model;

namespace WishlistBot.Jobs;

public delegate Task NotificationJobActionDelegate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId, int notificationId);
public delegate Task BroadcastJobActionDelegate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId, int broadcastId);
public delegate Task Legacy_JobActionDelegate<in TItem, in TObject>(ILogger logger, ITelegramBotClient client, UsersDb usersDb, TItem item, TObject obj);
