namespace WishlistBot.Queries.Profile;

public class SetProfileBirthdayQuery : IQuery
{
    public string Caption => "Дата рождения";
    public string Data => "@edit_profile_birthday";
}

