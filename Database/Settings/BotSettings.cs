using System.Text.Json.Serialization;
using WishlistBot.QueryParameters;

namespace WishlistBot.Database.Settings;

public class BotSettings : BasePropertyChanged
{
   private bool _sendNotifications;
   private bool _receiveNotifications;

   public bool SendNotifications
   {
      get => _sendNotifications;
      set => Set(ref _sendNotifications, value);
   }

   public bool ReceiveNotifications
   {
      get => _receiveNotifications;
      set => Set(ref _receiveNotifications, value);
   }

   [JsonConstructor]
   public BotSettings()
   {
   }
   
   public void SetFromEnum(SettingsEnum settingsEnum)
   {
      SendNotifications = settingsEnum.HasFlag(SettingsEnum.SendNotifications);
      ReceiveNotifications = settingsEnum.HasFlag(SettingsEnum.ReceiveNotifications);
   }

   public SettingsEnum ToEnum()
   {
      var settingsEnum = SettingsEnum.None;

      if (SendNotifications)
         settingsEnum |= SettingsEnum.SendNotifications;

      if (ReceiveNotifications)
         settingsEnum |= SettingsEnum.ReceiveNotifications;

      return settingsEnum;
   }
}
