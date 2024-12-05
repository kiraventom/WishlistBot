using System.Runtime.CompilerServices;

namespace WishlistBot.Database;

public abstract class BasePropertyChanged
{
   public event Action<BasePropertyChanged, string> PropertyChanged;

   protected void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
   {
      if (Equals(field, value))
         return;

      field = value;
      RaisePropertyChanged(propertyName);
   }

   protected void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, propertyName);
}
