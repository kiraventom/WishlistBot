using System.Text.Json;

namespace WishlistBot;

public static class CommonOptions
{
   public static JsonSerializerOptions Json = new()
   {
      WriteIndented = true, AllowTrailingCommas = true
   };
}
