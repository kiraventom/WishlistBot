using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.Queries.Settings;
using WishlistBot.QueryParameters;

namespace WishlistBot.BotMessages;

public class MainMenuMessage(ILogger logger) : BotMessage(logger)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard
         .AddButton<CompactListQuery>("Мои виши")
         .NewRow()
         .AddButton<MySubscriptionsQuery>()
         .AddButton<MySubscribersQuery>()
         .NewRow()
         .AddButton<SettingsQuery>();

      Text.Verbatim("Добро пожаловать в главное меню, ")
         .InlineMention(user)
         .Verbatim("!");

      const string mutedSpeaker = "\U0001f507";
      const string dot = "⋅ ";

      if (!user.Settings.ReceiveNotifications || !user.Settings.SendNotifications)
         Text.LineBreak();

      if (!user.Settings.ReceiveNotifications)
         Text.LineBreak().ItalicBold($"{dot}Получение уведомлений о вишах подписчиков: ").Verbatim(mutedSpeaker);

      if (!user.Settings.SendNotifications)
         Text.LineBreak().ItalicBold($"{dot}Отправка уведомлений о вишах подписчикам: ").Verbatim(mutedSpeaker);

      Text.LineBreak().LineBreak().Italic("Ссылку на вишлист можно скопировать в настройках");

      return Task.CompletedTask;
   }
}
