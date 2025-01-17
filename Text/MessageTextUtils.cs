using System.Text.RegularExpressions;
using WishlistBot.Database.Users;

namespace WishlistBot.Text;

public static partial class MessageTextUtils
{
   private static Regex GetDomainRegex { get; } = DomainRegex();

   public static string GetDomainFromLink(string link)
   {
      return GetDomainRegex.Replace(link, string.Empty);
   }

   public static string PriceToString(Price priceRange)
   {
      return priceRange switch
      {
         Price.NotSet => "не установлена",
         Price.Under300 => "до 300₽",
         Price.Under1000 => "от 300 до 1000₽",
         Price.Under3000 => "от 1000 до 3000₽",
         Price.Under5000 => "от 3000 до 5000₽",
         Price.Under10000 => "от 5000 до 10000₽",
         Price.Over10000 => "больше 10000₽",
         _ => "incorrect"
      };
   }

   public static string PriceToShortString(Price priceRange)
   {
      return priceRange switch
      {
         Price.NotSet => string.Empty,
         Price.Under300 => "<300₽",
         Price.Under1000 => "300-1000₽",
         Price.Under3000 => "1000-3000₽",
         Price.Under5000 => "3000-5000₽",
         Price.Under10000 => "5000-10000₽",
         Price.Over10000 => ">10000₽",
         _ => "incorrect"
      };
   }

   [GeneratedRegex(@".+\/\/|www.|\..+")]
   private static partial Regex DomainRegex();
}
