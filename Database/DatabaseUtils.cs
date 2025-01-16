namespace WishlistBot.Database;

public static class DatabaseUtils
{
   public static long GenerateId(IReadOnlyCollection<long> ids)
   {
      if (ids is null)
         throw new ArgumentException("Ids collection is null", nameof(ids));

      if (ids.Count == 0)
         return Random.Shared.Next();

      var id = ids.FirstOrDefault();
      while (ids.Contains(id))
      {
         id = Random.Shared.Next();
      }

      return id;
   }
}