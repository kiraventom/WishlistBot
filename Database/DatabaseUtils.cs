using System;
using System.Collections.Generic;

public static class DatabaseUtils
{
   public static long GenerateId(IReadOnlyCollection<long> ids)
   {
      if (ids is null)
         throw new ArgumentException("Ids collection is null", nameof(ids));

      if (!ids.Any())
         return (long)Random.Shared.Next();

      var id = ids.FirstOrDefault();
      while (ids.Contains(id))
      {
         id = (long)Random.Shared.Next();
      }

      return id;
   }
}
