using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Queries.Subscription;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class MySubscribersMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public MySubscribersMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

   // TODO: A lot of code is similar to MySubscriptionsMessage
#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      parameters.Pop(QueryParameterType.ReadOnly);
      parameters.Pop(QueryParameterType.SetUserTo);

      int currentPageIndex = 0;
      if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndex))
         currentPageIndex = (int)pageIndex;

      const int usersPerPage = 5;

      var subscribers = _usersDb.Values.Values
         .Where(u => u.Subscriptions.Contains(user.SubscribeId))
         .ToList();

      int pagesCount = (int)Math.Ceiling((double)subscribers.Count / usersPerPage);

      if (pagesCount == 0)
      {
         Text.Bold("У вас пока нет подписчиков :(");
         Keyboard.AddButton<MainMenuQuery>("В главное меню");
         return;
      }

      // Can happen if the only subscriber on the last page was removed
      if (currentPageIndex >= pagesCount)
         currentPageIndex = pagesCount - 1;

      for (int i = 0; i < usersPerPage; ++i)
      {
         var userIndex = currentPageIndex * usersPerPage + i;
         if (userIndex >= subscribers.Count)
            break;

         var subscriber = subscribers[userIndex];

         Keyboard.AddButton<SubscriberQuery>(
               subscriber.FirstName,
               QueryParameter.ReadOnly,
               new QueryParameter(QueryParameterType.SetUserTo, subscriber.SenderId), 
               new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex));

         Keyboard.NewRow();
      }

      if (currentPageIndex > 0)
         Keyboard.AddButton<MySubscribersQuery>("\u2b05\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex - 1));

      Keyboard.AddButton<MainMenuQuery>("В главное меню");

      if (currentPageIndex < pagesCount - 1)
         Keyboard.AddButton<MySubscribersQuery>("\u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex + 1));

      Text.Bold("Ваши подписчики:")
         .LineBreak().Bold($"Страница {currentPageIndex + 1} из {pagesCount}");
   }
}
