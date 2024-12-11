using System;
using System.Collections.Generic;

public static class DatabaseUtils
{
   public static long GenerateId(IReadOnlyCollection<long> ids)
   {
      if (ids == null || !ids.Any())
         throw new ArgumentException("Ids collection is null or empty", nameof(ids));

      var id = ids.First();
      while (ids.Contains(id))
      {
         id = (long)Random.Shared.Next();
      }

      return id;
   }
}
