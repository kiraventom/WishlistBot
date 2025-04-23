namespace WishlistBot.Jobs;

public interface Legacy_IJob : IDisposable
{
   event Action<Legacy_IJob, TaskStatus> Finished;

   object LinkedObject { get; }
   string Name { get; }

   void Cancel();
}
