using Serilog;
using Telegram.Bot;
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

    protected override Task Iterate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId)
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

    protected override async Task Iterate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId)
    {
        await _notificationAction.Invoke(logger, client, userContext, itemId, _notificationId);
    }

    public override void OnFinish()
    {
        using (var userContext = UserContext.Create())
        {
            var notification = userContext.Notifications.First(n => n.NotificationId == _notificationId);
            userContext.Notifications.Remove(notification);
            userContext.SaveChanges();
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
                await Task.Delay(_interval);
                _cts.Token.ThrowIfCancellationRequested();
                using (var userContext = UserContext.Create())
                {
                    await Iterate(logger, client, userContext, itemId);
                    userContext.SaveChanges();
                }
            }
        }, _cts.Token)
        .ContinueWith(t =>
        {
            if (t.Exception is not null)
                logger.Error("Job with SubjectId={subjectId} has faulted with exception {ex}", this.SubjectId, t.Exception.ToString());

            OnFinish();
            Finished?.Invoke(this, t.Status);
        });
    }

    public virtual void OnFinish()
    {
    }

    protected abstract Task Iterate(ILogger logger, ITelegramBotClient client, UserContext userContext, int itemId);

    public void Cancel() => _cts.Cancel();

    public void Dispose() => _cts.Dispose();
}
