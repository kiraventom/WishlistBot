using System.Collections;
using System.Reflection;

namespace WishlistBot.Model.User;

public class ViewerTargetBuilder
{
    private readonly Dictionary<Type, PropertyInfo> _propertiesCache = [];

    public static ViewerTargetBuilder Instance { get; } = new();

    private ViewerTargetBuilder() { }

    public T GetOrCreateViewerTarget<T>(UserModel userModel, int targetId) where T : IViewerTarget, new()
    {
        List<T> typedList;
        if (!_propertiesCache.TryGetValue(typeof(T), out var prop))
        {
            prop = userModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.PropertyType == typeof(List<T>)).FirstOrDefault();
            if (prop is null)
                throw new NotSupportedException($"Property of type 'List<{typeof(T).Name}>' not found in '{userModel.GetType().Name}'");

            _propertiesCache.Add(typeof(T), prop);
        }

        typedList = (List<T>)prop.GetValue(userModel);

        var target = typedList.FirstOrDefault(wvs => wvs.TargetId == targetId);
        if (target is null)
        {
            target = new T() { TargetId = targetId, ViewerId = userModel.UserId };
            typedList.Add(target);
        }

        return target;
    }
}
