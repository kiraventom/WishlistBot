namespace WishlistBot.Queries;

public class EditListQuery : IQuery
{
   public string Caption => "Редактировать";
   public string Data => "@edit_list";
}
