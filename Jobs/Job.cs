using Serilog;
using Telegram.Bot;
using WishlistBot.BotMessages;
using WishlistBot.Database.Users;
using WishlistBot.Model;

namespace WishlistBot.Jobs;

public class Job : IJob
{
    private readonly CancellationTokenSource _cts = new();

    private readonly int _broadcastId;
    private readonly BotMessage _botMessage;
    private readonly string _name;
    private readonly IReadOnlyCollection<int> _itemIds;
    private readonly TimeSpan _interval;
    private readonly MessageJobActionDelegate _messageAction;
    private readonly BroadcastJobActionDelegate _broadcastAction;

    private bool _started;

    public Job(BotMessage message, string name, IReadOnlyCollection<int> itemIds, TimeSpan interval, MessageJobActionDelegate action) : this(name, itemIds, interval)
    {
        _botMessage = message;
        _messageAction = action;
    }

    public Job(int broadcastId, string name, IReadOnlyCollection<int> itemIds, TimeSpan interval, BroadcastJobActionDelegate action) : this(name, itemIds, interval)
    {
        _broadcastId = broadcastId;
        _broadcastAction = action;
    }

    private Job(string name, IReadOnlyCollection<int> itemIds, TimeSpan interval)
    {
        _name = name;
        _itemIds = itemIds;
        _interval = interval;
    }

    int IJob.BroadcastId => _broadcastId;
    string IJob.Name => _name;

    public event Action<IJob, TaskStatus> Finished;

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
                       if (_botMessage is not null)
                       {
                            await _messageAction.Invoke(logger, client, userContext, itemId, _botMessage);
                       }
                       else
                       {
                            await _broadcastAction.Invoke(logger, client, userContext, itemId, _broadcastId);
                       }

                       userContext.SaveChanges();
                   }
               }
           }, _cts.Token)
           .ContinueWith(t => Finished?.Invoke(this, t.Status));
    }

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
