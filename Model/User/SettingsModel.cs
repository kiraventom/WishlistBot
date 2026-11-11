using System.ComponentModel.DataAnnotations;
using WishlistBot.QueryParameters;

namespace WishlistBot.Model.User;

public class SettingsModel
{
    [Key] public int SettingsId { get; set; }

    public int UserId { get; set; }
    public UserModel User { get; set; }

    public bool SendNotifications { get; set; }
    public bool ReceiveNotifications { get; set; }

    /// <summary>Напоминания о днях рождения друзей.</summary>
    public bool BirthdayNotifications { get; set; }

    public void SetFromEnum(SettingsEnum settingsEnum)
    {
        SendNotifications = settingsEnum.HasFlag(SettingsEnum.SendNotifications);
        ReceiveNotifications = settingsEnum.HasFlag(SettingsEnum.ReceiveNotifications);
        BirthdayNotifications = settingsEnum.HasFlag(SettingsEnum.BirthdayNotifications);
    }

    public SettingsEnum ToEnum()
    {
        var settingsEnum = SettingsEnum.None;

        if (SendNotifications)
            settingsEnum |= SettingsEnum.SendNotifications;

        if (ReceiveNotifications)
            settingsEnum |= SettingsEnum.ReceiveNotifications;

        if (BirthdayNotifications)
            settingsEnum |= SettingsEnum.BirthdayNotifications;

        return settingsEnum;
    }
}

