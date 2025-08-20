namespace WishlistBot;

public record HandleResult
{
    public bool Success { get; init; }
    public bool Cleanup { get; init; }

    public static implicit operator bool(HandleResult r) => r.Success;
    public static implicit operator HandleResult(bool b) => new HandleResult { Success = b };
}

