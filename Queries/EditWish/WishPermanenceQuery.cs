namespace WishlistBot.Queries.EditWish;

/// <summary>Переключение типа виша: открывает подтверждение «сделать постоянным/обычным».</summary>
public class WishPermanenceQuery : IQuery
{
   private const string starEmoji = "\u2b50";

   public string Caption => $"{starEmoji} Сделать постоянным";
   public string Data => "@wish_permanence";
}
