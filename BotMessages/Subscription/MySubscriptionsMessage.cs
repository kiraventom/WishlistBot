using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.Subscription;

public class MySubscriptionsMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public MySubscriptionsMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

   protected override void InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      parameters.Pop(QueryParameterType.ReadOnly);
      parameters.Pop(QueryParameterType.SetUserTo);

      int currentPageIndex = 0;
      if (parameters.Pop(QueryParameterType.SetListPageTo, out var pageIndex))
         currentPageIndex = (int)pageIndex;

      const int usersPerPage = 5;

      int pagesCount = (int)Math.Ceiling((double)user.Subscriptions.Count / usersPerPage);

      if (pagesCount == 0)
      {
         Text.Bold("Вы ещё ни на кого не подписаны :(");
         Keyboard.AddButton<MainMenuQuery>("В главное меню");
         return;
      }

      // Can happen if the only subscription on the last page was removed
      if (currentPageIndex >= pagesCount)
         currentPageIndex = pagesCount - 1;

      for (int i = 0; i < usersPerPage; ++i)
      {
         var userIndex = currentPageIndex * usersPerPage + i;
         if (userIndex >= user.Subscriptions.Count)
            break;

         var subscribeId = user.Subscriptions[userIndex];
         var userWeSubscribedTo = _usersDb.Values.Values.FirstOrDefault(u => u.SubscribeId == subscribeId);
         if (userWeSubscribedTo is null)
         {
            Logger.Error("Users DB does not contain user wish subscribe id [{subscribeId}]", subscribeId);
            return;
         }

         Keyboard.AddButton<CompactListQuery>(
               userWeSubscribedTo.FirstName,
               QueryParameter.ReadOnly,
               new QueryParameter(QueryParameterType.SetUserTo, userWeSubscribedTo.SenderId), 
               new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex));

         Keyboard.NewRow();
      }

      if (currentPageIndex > 0)
         Keyboard.AddButton<MySubscriptionsQuery>("\u2b05\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex - 1));

      Keyboard.AddButton<MainMenuQuery>("Назад");

      if (currentPageIndex < pagesCount - 1)
         Keyboard.AddButton<MySubscriptionsQuery>("\u27a1\ufe0f", new QueryParameter(QueryParameterType.SetListPageTo, currentPageIndex + 1));

      Text.Bold("Ваши подписки:")
         .LineBreak().Bold($"Страница {currentPageIndex + 1} из {pagesCount}");
   }
}
