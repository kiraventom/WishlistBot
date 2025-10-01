namespace WishlistBot.Queries.Profile;

public class ConfirmChangeProfileTypeQuery : IQuery
{
    public string Caption => "Тип профиля";
    public string Data => "@change_profile_type";
}

