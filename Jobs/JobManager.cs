using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;

namespace WishlistBot.Jobs;

public class JobManager
{
   private bool _inited;

   private ILogger _logger;
   private ITelegramBotClient _client;
   private UsersDb _usersDb;

   public static JobManager Instance { get; } = new();

   private Dictionary<int, IJob> ActiveJobs { get; } = new();

   public void Init(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      if (_inited)
         return;

      _logger = logger;
      _client = client;
      _usersDb = usersDb;
      _inited = true;
   }

   public bool IsJobActive(object linkedObject) => ActiveJobs.ContainsKey(linkedObject.GetHashCode());

   public string GetActiveJobName(object linkedObject) => ActiveJobs[linkedObject.GetHashCode()].Name;

   public void StartJob<TItem, TObject>(string name, TObject linkedObject, IEnumerable<TItem> collection, TimeSpan interval, JobActionDelegate<TItem, TObject> action)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(linkedObject);
      ArgumentNullException.ThrowIfNull(collection);
      ArgumentNullException.ThrowIfNull(action);

      if (ActiveJobs.ContainsKey(linkedObject.GetHashCode()))
      {
         throw new NotSupportedException($"Can't start two jobs on the same object [{linkedObject.GetHashCode()}]");
      }

      var job = new Job<TItem, TObject>(linkedObject, name, collection, interval, action);
      ActiveJobs.Add(linkedObject.GetHashCode(), job);

      job.Finished += OnJobFinished;
      job.Start(_logger, _client, _usersDb);

      _logger.Information("Started job linked to object [{hashcode}]", linkedObject.GetHashCode());
   }

   public void StopJob(object linkedObject)
   {
      ArgumentNullException.ThrowIfNull(linkedObject);

      if (ActiveJobs.TryGetValue(linkedObject.GetHashCode(), out var job))
      {
         job.Cancel();
      }
      else
      {
         _logger.Warning("Attempt to stop job that is not running, object [{hashCode}]", linkedObject.GetHashCode());
      }
   }

   private void OnJobFinished(IJob job, TaskStatus status)
   {
      if (status != TaskStatus.RanToCompletion)
      {
         _logger.Error("Job linked to object [{hash}] has finished with status '{status}'", job.LinkedObject.GetHashCode(), status.ToString());
      }
      else
      {
         _logger.Information("Job linked to object [{hash}] has finished with status '{status}'", job.LinkedObject.GetHashCode(), status.ToString());
      }

      job.Finished -= OnJobFinished;
      ActiveJobs.Remove(job.LinkedObject.GetHashCode());
      job.Dispose();
   }
}
