namespace WishlistBot.Queries.Settings;

public class ConfirmRegenerateLinkQuery : IQuery
{
   public string Caption => "Перегенерировать ссылку";
   public string Data => "@regen_link";
}
