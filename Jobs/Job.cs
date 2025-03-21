using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;

namespace WishlistBot.Jobs;

public class Job<TItem, TObject>(TObject linkedObject, string name, IEnumerable<TItem> items, TimeSpan interval, JobActionDelegate<TItem, TObject> action) : IJob
{
   private readonly CancellationTokenSource _cts = new();
   private bool _started;

   object IJob.LinkedObject => linkedObject;
   string IJob.Name => name;

   public event Action<IJob, TaskStatus> Finished;

   public void Start(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      if (_started)
         throw new NotSupportedException("Attemt to start job twice");

      _started = true;

      Task.Run(async () =>
         {
            foreach (var item in items)
            {
               _cts.Token.ThrowIfCancellationRequested();
               await Task.Delay(interval);
               await action.Invoke(logger, client, usersDb, item, linkedObject);
            }
         }, _cts.Token)
         .ContinueWith(t => Finished?.Invoke(this, t.Status));
   }

   public void Cancel() => _cts.Cancel();

   public void Dispose() => _cts.Dispose();
}
