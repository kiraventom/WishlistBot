namespace WishlistBot.Jobs;

public interface IJob : IDisposable
{
   event Action<IJob, TaskStatus> Finished;

   int BroadcastId { get; }
   string Name { get; }

   void Cancel();
}

public interface Legacy_IJob : IDisposable
{
   event Action<Legacy_IJob, TaskStatus> Finished;

   object LinkedObject { get; }
   string Name { get; }

   void Cancel();
}
