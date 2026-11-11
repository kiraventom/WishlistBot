using System.ComponentModel.DataAnnotations;
using WishlistBot.QueryParameters;

namespace WishlistBot.Model.User;

public class SettingsModel
{
    [Key] public int SettingsId { get; set; }

    public int UserId { get; set; }
    public UserModel User { get; set; }

    public bool SendNotifications { get; set; }
    public bool ReceiveFriendWishNotifications { get; set; }

    /// <summary>Напоминания о днях рождения друзей.</summary>
    public bool BirthdayNotifications { get; set; }

    /// <summary>Напоминания о чистке вишлиста после праздников.</summary>
    public bool WishlistCleanupNotifications { get; set; }

    /// <summary>Напоминания заполнить вишлист перед праздником.</summary>
    public bool WishlistFillNotifications { get; set; }

    public void SetFromEnum(SettingsEnum settingsEnum)
    {
        SendNotifications = settingsEnum.HasFlag(SettingsEnum.SendNotifications);
        ReceiveFriendWishNotifications = settingsEnum.HasFlag(SettingsEnum.ReceiveFriendWishNotifications);
        BirthdayNotifications = settingsEnum.HasFlag(SettingsEnum.BirthdayNotifications);
        WishlistCleanupNotifications = settingsEnum.HasFlag(SettingsEnum.WishlistCleanupNotifications);
        WishlistFillNotifications = settingsEnum.HasFlag(SettingsEnum.WishlistFillNotifications);
    }

    public SettingsEnum ToEnum()
    {
        var settingsEnum = SettingsEnum.None;

        if (SendNotifications)
            settingsEnum |= SettingsEnum.SendNotifications;

        if (ReceiveFriendWishNotifications)
            settingsEnum |= SettingsEnum.ReceiveFriendWishNotifications;

        if (BirthdayNotifications)
            settingsEnum |= SettingsEnum.BirthdayNotifications;

        if (WishlistCleanupNotifications)
            settingsEnum |= SettingsEnum.WishlistCleanupNotifications;

        if (WishlistFillNotifications)
            settingsEnum |= SettingsEnum.WishlistFillNotifications;

        return settingsEnum;
    }
}

