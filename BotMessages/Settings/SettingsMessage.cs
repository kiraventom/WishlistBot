using Serilog;
using WishlistBot.Queries;
using WishlistBot.Database.Users;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages.Settings;

[AllowedTypes(QueryParameterType.SetSettingsTo, QueryParameterType.RegenerateLink)]
public class SettingsMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      if (parameters.Pop(QueryParameterType.SetSettingsTo, out var newSettingsNum))
      {
         var newSettingsEnum = (SettingsEnum)newSettingsNum;
         user.Settings.SetFromEnum(newSettingsEnum);
      }

      if (parameters.Pop(QueryParameterType.RegenerateLink))
      {
         var newSubscribeId = Guid.NewGuid().ToString("N");
         var subscribers = Users.Where(u => u.Subscriptions.Contains(user.SubscribeId));
         foreach (var subscriber in subscribers)
         {
            subscriber.Subscriptions.Remove(user.SubscribeId);
            subscriber.Subscriptions.Add(newSubscribeId);
         }

         user.SubscribeId = newSubscribeId;
      }

      const string enabledSpeaker = "\U0001f50a";
      const string mutedSpeaker = "\U0001f507";
      const string enabledStr = "Вкл.";
      const string mutedStr = "Выкл.";
      const string dot = "⋅ ";

      var receiveNotificationsEmoji = user.Settings.ReceiveNotifications ? enabledSpeaker : mutedSpeaker;
      var sendNotificationsEmoji = user.Settings.SendNotifications ? enabledSpeaker : mutedSpeaker;

      var receiveNotificationsStr = user.Settings.ReceiveNotifications ? enabledStr : mutedStr;
      var sendNotificationsStr = user.Settings.SendNotifications ? enabledStr : mutedStr;

      Text
         .Bold("Настройки бота:")
         .LineBreak().Bold(dot).Bold("Получение уведомлений: ").Verbatim(receiveNotificationsStr)
         .LineBreak().Bold(dot).Bold("Отправка уведомлений: ").Verbatim(sendNotificationsStr)
         .LineBreak().Bold(dot).Bold("Ссылка на ваш вишлист").Italic(" (нажмите, чтобы скопировать)").Bold(":")
         .LineBreak().Monospace($"t.me/SmartWishlistBot?start={user.SubscribeId}");

      var settingsEnum = user.Settings.ToEnum();

      Keyboard.AddButton<SettingsQuery>(
         $"Получение уведомлений: {receiveNotificationsEmoji}",
         new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.ReceiveNotifications)));

      Keyboard.NewRow().AddButton<SettingsQuery>(
         $"Отправка уведомлений: {sendNotificationsEmoji}",
         new QueryParameter(QueryParameterType.SetSettingsTo, (long)(settingsEnum ^ SettingsEnum.SendNotifications)));

      Keyboard.NewRow().AddButton<ConfirmRegenerateLinkQuery>("Изменить ссылку на вишлист");
      Keyboard.NewRow().AddButton<MainMenuQuery>("В главное меню");

      return Task.CompletedTask;
   }
}
