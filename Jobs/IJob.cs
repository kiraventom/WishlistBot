using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;

namespace WishlistBot.Jobs;

public interface IJob : IDisposable
{
   event Action<IJob, TaskStatus> Finished;

   object LinkedObject { get; }
   string Name { get; }

   void Start(ILogger logger, ITelegramBotClient client, UsersDb usersDb);
   void Cancel();
}
