namespace WishlistBot.Queries.Profile;

public class SetProfileNotesQuery : IQuery
{
    public string Caption => "Заметки";
    public string Data => "@edit_profile_notes";
}

