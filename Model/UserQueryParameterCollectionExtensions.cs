using Microsoft.EntityFrameworkCore;
using WishlistBot.QueryParameters;

namespace WishlistBot.Model;

public static class UserQueryParameterCollectionExtensions
{
   public static UserModel GetUser(this QueryParameterCollection parameters, UserContext userContext, bool noTracking = false)
   {

      var users = userContext.Users;
      if (noTracking)
          return users.AsNoTracking().FirstOrDefault(u => u.UserId == userId);
      else
          return users.FirstOrDefault(u => u.UserId == userId);
   }
}

