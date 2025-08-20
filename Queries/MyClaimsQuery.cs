namespace WishlistBot.Queries;

public class MyClaimsQuery : IQuery
{
    private const string lockEmoji = "\U0001F512";

    public string Caption => $"{lockEmoji} Брони";
    public string Data => "@my_claims";
}

