namespace WishlistBot.Model.User;

public interface IViewerTarget
{
    int TargetId { get; set; }
    int ViewerId { get; set; }
}

