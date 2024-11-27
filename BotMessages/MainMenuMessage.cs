using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Subscription;

namespace WishlistBot.BotMessages;

public class MainMenuMessage : BotMessage
{
   public MainMenuMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<CompactListQuery>("Мои виши")
         .AddButton<MySubscriptionsQuery>()
         .AddButton("@settings", "Настройки");

      Text.Italic("Добро пожаловать в главное меню, ")
         .InlineMention(user.FirstName, user.SenderId)
         .Italic("!")
         .LineBreak()
         .LineBreak().Bold("Ссылка на ваш вишлист")
         .Italic(" (нажмите, чтобы скопировать)")
         .Bold(":")
         .LineBreak().Monospace($"t.me/smartwishlistbot?start={user.SubscribeId}");
   }
}
