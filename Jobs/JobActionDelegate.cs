using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;

namespace WishlistBot.Jobs;

public delegate Task JobActionDelegate<in TItem, in TObject>(ILogger logger, ITelegramBotClient client, UsersDb usersDb, TItem item, TObject obj);
