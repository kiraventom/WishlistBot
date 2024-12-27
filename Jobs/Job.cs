using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Database.Users;
using WishlistBot.Database.Admin;

namespace WishlistBot.Jobs;

public class Job<TItem, TObject>(TObject linkedObject, IEnumerable<TItem> items, TimeSpan interval, JobActionDelegate<TItem, TObject> action) : IJob
{
   private readonly CancellationTokenSource _cts = new();
   private bool _started;
   private Task _task;

   object IJob.LinkedObject => (object)linkedObject;

   public event Action<IJob, TaskStatus> Finished;

   public void Start(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      if (_started)
         throw new NotSupportedException("Attemt to start job twice");

      _started = true;

      _task = Task.Run(async () => 
      {
         foreach (var item in items)
         {
            _cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(interval);
            await action.Invoke(logger, client, usersDb, item, linkedObject);
         }
      }, _cts.Token)
      .ContinueWith(t => Finished.Invoke(this, t.Status));
   }

   public void Cancel()
   {
      _cts.Cancel();
   }

   public void Dispose()
   {
      _cts.Dispose();
   }
}
