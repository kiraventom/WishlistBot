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

   private Dictionary<int, BroadcastJob> BroadcastJobs { get; } = new();
   private Dictionary<int, NotificationJob> NotificationJobs { get; } = new();

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

   // TODO: Fix this mess
   public bool IsJobActive(int broadcastId) => BroadcastJobs.ContainsKey(broadcastId);
   public bool Legacy_IsJobActive(object linkedObject) => BroadcastJobs.ContainsKey(linkedObject.GetHashCode());

   public string GetActiveJobName(int broadcastId) => BroadcastJobs[broadcastId].Name;
   public string Legacy_GetActiveJobName(object linkedObject) => BroadcastJobs[linkedObject.GetHashCode()].Name;

   public void StartJob(string name, int notificationId, IReadOnlyCollection<int> itemIds, TimeSpan interval, NotificationJobActionDelegate action)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(action);

      if (NotificationJobs.ContainsKey(notificationId))
      {
         throw new NotSupportedException($"Can't start two jobs on the same notification [{notificationId}]");
      }

      var job = new NotificationJob(notificationId, name, itemIds, interval, action);
      NotificationJobs.Add(notificationId, job);

      job.Finished += OnNotificationJobFinished;
      job.Start(_logger, _client);

      _logger.Information("Started job on notification [{notificationId}]", notificationId);
   }

   public void StartJob(string name, int broadcastId, IReadOnlyCollection<int> itemIds, TimeSpan interval, BroadcastJobActionDelegate action)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(action);

      if (BroadcastJobs.ContainsKey(broadcastId))
      {
         throw new NotSupportedException($"Can't start two jobs on the same broadcast [{broadcastId}]");
      }

      var job = new BroadcastJob(broadcastId, name, itemIds, interval, action);
      BroadcastJobs.Add(broadcastId, job);

      job.Finished += OnBroadcastJobFinished;
      job.Start(_logger, _client);

      _logger.Information("Started job on broadcast [{broadcastId}]", broadcastId);
   }

   public void Legacy_StartJob<TItem, TObject>(string name, TObject linkedObject, IEnumerable<TItem> collection, TimeSpan interval, Legacy_JobActionDelegate<TItem, TObject> action)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(linkedObject);
      ArgumentNullException.ThrowIfNull(collection);
      ArgumentNullException.ThrowIfNull(action);

      if (BroadcastJobs.ContainsKey(linkedObject.GetHashCode()))
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
      if (BroadcastJobs.TryGetValue(broadcastId, out var job))
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

   private void OnBroadcastJobFinished(Job job, TaskStatus status)
   {
      if (status != TaskStatus.RanToCompletion)
      {
         _logger.Error("Job linked to broadcast [{broadcastId}] has finished with status '{status}'", job.SubjectId, status.ToString());
      }
      else
      {
         _logger.Information("Job linked to broadcast [{broadcastId}] has finished with status '{status}'", job.SubjectId, status.ToString());
      }

      job.Finished -= OnBroadcastJobFinished;
      BroadcastJobs.Remove(job.SubjectId);
      job.Dispose();
   }

   private void OnNotificationJobFinished(Job job, TaskStatus status)
   {
      if (status != TaskStatus.RanToCompletion)
      {
         _logger.Error("Job linked to notification [{notificationId}] has finished with status '{status}'", job.SubjectId, status.ToString());
      }
      else
      {
         _logger.Information("Job linked to notification [{notificationId}] has finished with status '{status}'", job.SubjectId, status.ToString());
      }

      job.Finished -= OnNotificationJobFinished;
      NotificationJobs.Remove(job.SubjectId);
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
