using Serilog;
using Telegram.Bot;

namespace WishlistBot.Jobs;

public class JobManager
{
   private bool _inited;

   private ILogger _logger;
   private ITelegramBotClient _client;

   public static JobManager Instance { get; } = new();

   private Dictionary<int, BroadcastJob> BroadcastJobs { get; } = new();
   private Dictionary<int, NotificationJob> NotificationJobs { get; } = new();

   public void Init(ILogger logger, ITelegramBotClient client)
   {
      if (_inited)
         return;

      _logger = logger;
      _client = client;
      _inited = true;
   }

   // TODO: Fix this mess
   public bool IsBroadcastJobActive(int broadcastId) => BroadcastJobs.ContainsKey(broadcastId);
   public string GetActiveBroadcastJobName(int broadcastId) => BroadcastJobs[broadcastId].Name;

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

   public void StopNotificationJob(int notificationId)
   {
      if (NotificationJobs.TryGetValue(notificationId, out var job))
      {
         job.Cancel();
      }
      else
      {
         _logger.Warning("Attempt to stop job that is not running, notificationId [{notificationId}]", notificationId);
      }
   }

   public void StopBroadcastJob(int broadcastId)
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
}
