using System.ComponentModel.DataAnnotations;

namespace WishlistBot.Model.User;

public class BroadcastModel
{
    [Key] public int BroadcastId { get; set; }

    [Required] public string Text { get; set; }
    public string FileId { get; set; }
    public DateTime? DateTimeSent { get; set; }
    public bool Deleted { get; set; }

    public string GetShortText()
    {
        if (Text is null)
            return "<empty>";

        return Text.Length <= 20
           ? Text
           : Text[..19] + "…";
    }
}

