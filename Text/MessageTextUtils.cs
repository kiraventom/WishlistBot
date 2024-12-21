using System.Text.RegularExpressions;

namespace WishlistBot.Text;

public static class MessageTextUtils
{
   private static Regex GetDomainRegex { get; }= new Regex(@".+\/\/|www.|\..+");

   public static string GetDomainFromLink(string link)
   {
      return GetDomainRegex.Replace(link, string.Empty);
   }
}
