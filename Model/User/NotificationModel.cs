using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using WishlistBot.Notification;

namespace WishlistBot.Model.User;

public class NotificationModel
{
    [Key] public int NotificationId { get; set; }
    public int SourceId { get; set; }

    public NotificationMessageType Type { get; set; }
    public int? SubjectId { get; set; }
    public string Extra { get; set; }

    [ForeignKey(nameof(SourceId))]
    public UserModel Source { get; set; }

    public bool GetExtraBool()
    {
        if (Extra is null)
            return false;

        var byteArray = Convert.FromBase64String(Extra);
        return BitConverter.ToBoolean(byteArray);
    }

    public void SetExtraBool(bool value)
    {
        var byteArray = BitConverter.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }

    public int GetExtraInt()
    {
        if (Extra is null)
            return -1;

        var byteArray = Convert.FromBase64String(Extra);
        return BitConverter.ToInt32(byteArray);
    }

    public void SetExtraInt(int value)
    {
        var byteArray = BitConverter.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }

    public double GetExtraDouble()
    {
        if (Extra is null)
            return double.NaN;

        var byteArray = Convert.FromBase64String(Extra);
        return BitConverter.ToDouble(byteArray);
    }

    public void SetExtraDouble(double value)
    {
        var byteArray = BitConverter.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }

    public string GetExtraString()
    {
        if (Extra is null)
            return null;

        var byteArray = Convert.FromBase64String(Extra);
        return Encoding.UTF8.GetString(byteArray);
    }

    public void SetExtraString(string value)
    {
        var byteArray = Encoding.UTF8.GetBytes(value);
        Extra = Convert.ToBase64String(byteArray);
    }
}

