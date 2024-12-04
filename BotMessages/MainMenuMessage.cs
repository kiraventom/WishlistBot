using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

public class MainMenuMessage(ILogger logger) : BotMessage(logger)
{
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<CompactListQuery>("Мои виши")
         .NewRow()
         .AddButton<MySubscriptionsQuery>()
         .AddButton<MySubscribersQuery>()
         .NewRow()
         .AddButton("@settings", "Настройки");

      Text.Italic("Добро пожаловать в главное меню, ")
         .InlineMention(user)
         .Italic("!")
         .LineBreak()
         .LineBreak().Bold("Ссылка на ваш вишлист")
         .Italic(" (нажмите, чтобы скопировать)")
         .Bold(":")
         .LineBreak().Monospace($"t.me/smartwishlistbot?start={user.SubscribeId}");
   }
}
