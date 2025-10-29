using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WishlistBot.Model.User;

public class OrderAssignmentInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null)
            return result;

        var addedEntries = eventData.Context.ChangeTracker.Entries<WishModel>().Where(e => e.State == EntityState.Added);
        var wishes = eventData.Context.Set<WishModel>().Local;
        foreach (var entry in addedEntries)
        {
            if (entry.Entity.Order > 0)
                continue;

            var sameUserWishes = wishes.Where(w => w.OwnerId == entry.Entity.OwnerId);

            if (wishes.Any())
                entry.Entity.Order = sameUserWishes.Max(w => w.Order) + 1;
            else
                entry.Entity.Order = 0;
        }

        return result;
    }
}

