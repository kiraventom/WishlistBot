using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
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
         /*.NewRow()
         .AddButton("@settings", "Настройки")*/;

      Text.Italic("Добро пожаловать в главное меню, ")
         .InlineMention(user)
         .Italic("!")
         .LineBreak()
         .LineBreak().Bold("Ссылка на ваш вишлист")
         .Italic(" (нажмите, чтобы скопировать)")
         .Bold(":")
         .LineBreak().Monospace($"t.me/smartwishlistbot?start={user.SubscribeId}");

      return Task.CompletedTask;
   }
}
