using Serilog;
using Telegram.Bot;
using WishlistBot.Database.Users;
using WishlistBot.Model;

namespace WishlistBot.Jobs;

public class BroadcastJob : Job
{
    private readonly int _broadcastId;
    private readonly BroadcastJobActionDelegate _broadcastAction;

    public override int SubjectId => _broadcastId;

    public BroadcastJob(int broadcastId, string name, IReadOnlyCollection<int> itemIds, TimeSpan interval, BroadcastJobActionDelegate action) : base(name, itemIds, interval)
    {
        _broadcastId = broadcastId;
        _broadcastAction = action;
    }

    protected override Task StartInternal(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId)
    {
        return _broadcastAction.Invoke(logger, client, userContext, itemId, _broadcastId);
    }
}

public class NotificationJob : Job
{
    private readonly int _notificationId;
    private readonly NotificationJobActionDelegate _notificationAction;

    public override int SubjectId => _notificationId;

    public NotificationJob(int notificationId, string name, IReadOnlyCollection<int> itemIds, TimeSpan interval, NotificationJobActionDelegate action) : base(name, itemIds, interval)
    {
        _notificationId = notificationId;
        _notificationAction = action;
    }

    protected override async Task StartInternal(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId)
    {
        try
        {
            await _notificationAction.Invoke(logger, client, userContext, itemId, _notificationId);
        }
        finally
        {
            var notification = userContext.Notifications.First(n => n.NotificationId == _notificationId);
            userContext.Notifications.Remove(notification);
        }
    }
}

public abstract class Job
{
    private readonly CancellationTokenSource _cts = new();

    private readonly IReadOnlyCollection<int> _itemIds;
    private readonly TimeSpan _interval;

    private bool _started;

    public abstract int SubjectId { get; }
    public string Name { get; private set; }

    protected Job(string name, IReadOnlyCollection<int> itemIds, TimeSpan interval)
    {
        Name = name;
        _itemIds = itemIds;
        _interval = interval;
    }

    public event Action<Job, TaskStatus> Finished;

    public void Start(ILogger logger, ITelegramBotClient client)
    {
        if (_started)
            throw new NotSupportedException("Attemt to start job twice");

        _started = true;

        Task.Run(async () =>
        {
            foreach (var itemId in _itemIds)
            {
                _cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(_interval);
                using (var userContext = UserContext.Create())
                {
                    await StartInternal(logger, client, userContext, itemId);
                    userContext.SaveChanges();
                }
            }
        }, _cts.Token)
        .ContinueWith(t =>
        {
            if (t.Exception is not null)
                logger.Error("Job with SubjectId={subjectId} has faulted with exception {ex}", this.SubjectId, t.Exception.ToString());

            Finished?.Invoke(this, t.Status);
        });
    }

    protected abstract Task StartInternal(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId);

    public void Cancel() => _cts.Cancel();

    public void Dispose() => _cts.Dispose();
}

public class Legacy_Job<TItem, TObject>(TObject linkedObject, string name, IEnumerable<TItem> items, TimeSpan interval, Legacy_JobActionDelegate<TItem, TObject> action) : Legacy_IJob
{
    private readonly CancellationTokenSource _cts = new();
    private bool _started;

    object Legacy_IJob.LinkedObject => linkedObject;
    string Legacy_IJob.Name => name;

    public event Action<Legacy_IJob, TaskStatus> Finished;

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
