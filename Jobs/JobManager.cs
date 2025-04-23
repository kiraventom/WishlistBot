using Serilog;
using Telegram.Bot;
using WishlistBot.BotMessages;
using WishlistBot.Database.Users;
using WishlistBot.Model;

namespace WishlistBot.Jobs;

// TODO Replace broadcastId with jobId, add Jobs table to Context
public class JobManager
{
   private bool _inited;

   private ILogger _logger;
   private ITelegramBotClient _client;
   private UsersDb _usersDb;

   public static JobManager Instance { get; } = new();

   private Dictionary<int, IJob> ActiveJobs { get; } = new();
   private Dictionary<int, Legacy_IJob> Legacy_ActiveJobs { get; } = new();

   public void Init(ILogger logger, ITelegramBotClient client, UsersDb usersDb)
   {
      if (_inited)
         return;

      _logger = logger;
      _client = client;
      _usersDb = usersDb;
      _inited = true;
   }

   public bool IsJobActive(int broadcastId) => ActiveJobs.ContainsKey(broadcastId);
   public bool Legacy_IsJobActive(object linkedObject) => ActiveJobs.ContainsKey(linkedObject.GetHashCode());

   public string GetActiveJobName(int broadcastId) => ActiveJobs[broadcastId].Name;
   public string Legacy_GetActiveJobName(object linkedObject) => ActiveJobs[linkedObject.GetHashCode()].Name;

   public void StartJob(string name, BotMessage botMessage, IReadOnlyCollection<int> itemIds, TimeSpan interval, MessageJobActionDelegate action)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(action);

      // TODO
      /* if (ActiveJobs.ContainsKey(broadcastId)) */
      /* { */
      /*    throw new NotSupportedException($"Can't start two jobs on the same broadcast [{broadcastId}]"); */
      /* } */

      var job = new Job(botMessage, name, itemIds, interval, action);
      ActiveJobs.Add(broadcastId, job);

      job.Finished += OnJobFinished;
      job.Start(_logger, _client);

      _logger.Information("Started job on broadcast [{broadcastId}]", broadcastId);
   }

   public void StartJob(string name, int broadcastId, IReadOnlyCollection<int> itemIds, TimeSpan interval, BroadcastJobActionDelegate action)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(action);

      if (ActiveJobs.ContainsKey(broadcastId))
      {
         throw new NotSupportedException($"Can't start two jobs on the same broadcast [{broadcastId}]");
      }

      var job = new Job(broadcastId, name, itemIds, interval, action);
      ActiveJobs.Add(broadcastId, job);

      job.Finished += OnJobFinished;
      job.Start(_logger, _client);

      _logger.Information("Started job on broadcast [{broadcastId}]", broadcastId);
   }

   public void Legacy_StartJob<TItem, TObject>(string name, TObject linkedObject, IEnumerable<TItem> collection, TimeSpan interval, Legacy_JobActionDelegate<TItem, TObject> action)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(linkedObject);
      ArgumentNullException.ThrowIfNull(collection);
      ArgumentNullException.ThrowIfNull(action);

      if (ActiveJobs.ContainsKey(linkedObject.GetHashCode()))
      {
         throw new NotSupportedException($"Can't start two jobs on the same object [{linkedObject.GetHashCode()}]");
      }

      var job = new Legacy_Job<TItem, TObject>(linkedObject, name, collection, interval, action);
      Legacy_ActiveJobs.Add(linkedObject.GetHashCode(), job);

      job.Finished += Legacy_OnJobFinished;
      job.Start(_logger, _client, _usersDb);

      _logger.Information("Started job linked to object [{hashcode}]", linkedObject.GetHashCode());
   }

   public void StopJob(int broadcastId)
   {
      if (ActiveJobs.TryGetValue(broadcastId, out var job))
      {
         job.Cancel();
      }
      else
      {
         _logger.Warning("Attempt to stop job that is not running, broadcastId [{broadcastId}]", broadcastId);
      }
   }

   public void Legacy_StopJob(object linkedObject)
   {
      ArgumentNullException.ThrowIfNull(linkedObject);

      if (Legacy_ActiveJobs.TryGetValue(linkedObject.GetHashCode(), out var job))
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
         _logger.Error("Job linked to broadcast [{broadcastId}] has finished with status '{status}'", job.BroadcastId, status.ToString());
      }
      else
      {
         _logger.Information("Job linked to broadcast [{broadcastId}] has finished with status '{status}'", job.BroadcastId, status.ToString());
      }

      job.Finished -= OnJobFinished;
      ActiveJobs.Remove(job.BroadcastId);
      job.Dispose();
   }

   private void Legacy_OnJobFinished(Legacy_IJob job, TaskStatus status)
   {
      if (status != TaskStatus.RanToCompletion)
      {
         _logger.Error("Job linked to object [{hash}] has finished with status '{status}'", job.LinkedObject.GetHashCode(), status.ToString());
      }
      else
      {
         _logger.Information("Job linked to object [{hash}] has finished with status '{status}'", job.LinkedObject.GetHashCode(), status.ToString());
      }

      job.Finished -= Legacy_OnJobFinished;
      Legacy_ActiveJobs.Remove(job.LinkedObject.GetHashCode());
      job.Dispose();
   }
}
