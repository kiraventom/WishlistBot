namespace WishlistBot.Jobs;

public interface IJob : IDisposable
{
   event Action<IJob, TaskStatus> Finished;

   object LinkedObject { get; }
   string Name { get; }

   void Cancel();
}
