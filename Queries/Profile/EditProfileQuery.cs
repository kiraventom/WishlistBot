namespace WishlistBot.Queries.Profile;

public class EditProfileQuery : IQuery
{
    private const string silhouette = "\U0001f464";

    public string Caption => $"{silhouette} Профиль";
    public string Data => "@edit_profile";
}
