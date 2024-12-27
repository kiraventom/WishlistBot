using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;

namespace WishlistBot.Jobs;

public delegate Task JobActionDelegate<TItem, TObject>(ILogger logger, ITelegramBotClient client, UsersDb usersDb, TItem item, TObject obj);
