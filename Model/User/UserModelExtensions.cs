namespace WishlistBot.Model.User;

public static class UserModelExtensions
{
    public static T GetOrCreateViewerTarget<T>(this UserModel userModel, int targetId) where T : IViewerTarget, new() => ViewerTargetBuilder.Instance.GetOrCreateViewerTarget<T>(userModel, targetId);

    public static IOrderedEnumerable<WishModel> GetSortedWishes(this UserModel userModel, WishViewSettingsModel viewSettings)
    {
        var descending = viewSettings?.Descending ?? false;
        var property = viewSettings?.SortProperty ?? SortProperty.Default;
        var onlyUnclaimed = viewSettings?.OnlyUnclaimed ?? false;

        return property switch
        {
            SortProperty.Default when descending && onlyUnclaimed => userModel.Wishes.Where(w => w.ClaimerId == null).OrderByDescending(w => w.Order),
            SortProperty.Price when descending && onlyUnclaimed => userModel.Wishes.Where(w => w.ClaimerId == null).OrderByDescending(w => ((int)w.PriceRange)),
            SortProperty.Default when descending => userModel.Wishes.OrderByDescending(w => w.Order),
            SortProperty.Price when descending => userModel.Wishes.OrderByDescending(w => ((int)w.PriceRange)),
            SortProperty.Default when onlyUnclaimed => userModel.Wishes.Where(w => w.ClaimerId == null).OrderBy(w => w.Order),
            SortProperty.Price when onlyUnclaimed => userModel.Wishes.Where(w => w.ClaimerId == null).OrderBy(w => ((int)w.PriceRange)),
            SortProperty.Default => userModel.Wishes.OrderBy(w => w.Order),
            SortProperty.Price => userModel.Wishes.OrderBy(w => ((int)w.PriceRange)),
            _ => userModel.Wishes.OrderByDescending(w => w.Order)
        };
    }

    public static List<WishModel> GetSortedClaimedWishes(this UserModel userModel)
    {
        userModel.ClaimedWishes.Sort((w0, w1) => w0.Order.CompareTo(w1.Order));
        return userModel.ClaimedWishes;
    }

    public static string GetSubscribeLink(this UserModel userModel) => $"https://t.me/{Config.Instance.Username}?start={userModel.SubscribeId}";
}


