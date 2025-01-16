using System.Text.RegularExpressions;

namespace WishlistBot.Text;

public static partial class MessageTextUtils
{
   private static Regex GetDomainRegex { get; } = DomainRegex();

   public static string GetDomainFromLink(string link)
   {
      return GetDomainRegex.Replace(link, string.Empty);
   }

   [GeneratedRegex(@".+\/\/|www.|\..+")]
   private static partial Regex DomainRegex();
}
