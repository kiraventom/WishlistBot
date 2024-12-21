using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages;

[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.ReadOnly, QueryParameterType.SetListPageTo)]
[ChildMessage(typeof(CompactListMessage))]
public class FullListMessage(ILogger logger, UsersDb usersDb) : UserBotMessage(logger, usersDb)
{
   protected override Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      var isReadOnly = parameters.Peek(QueryParameterType.ReadOnly);
      user = GetUser(user, parameters);

      var totalCount = user.Wishes.Count;

      ListMessageUtils.AddListControls<FullListQuery, CompactListQuery>(Keyboard, parameters, totalCount, (itemIndex, pageIndex) =>
      {
         if (isReadOnly)
            AddShowWishButton(user, itemIndex, pageIndex);
         else
            AddEditWishButton(user, itemIndex, pageIndex);
      });

      if (totalCount == 0)
      {
         Text.Bold("Список пуст");
         return Task.CompletedTask;
      }

      if (isReadOnly)
         Text.Bold("Виши ").InlineMention(user).Bold(":");
      else
         Text.Bold("Ваши виши:");

      return Task.CompletedTask;
   }

   private void AddShowWishButton(BotUser user, int itemIndex, int pageIndex)
   {
      var wish = user.Wishes[itemIndex];

      const string eyeEmoji = "\U0001f441\ufe0f ";

      var isClaimed = wish.ClaimerId != 0;
      var claimedText = isClaimed ? "[БРОНЬ] " : string.Empty;

      Keyboard.AddButton<ShowWishQuery>(
         claimedText + eyeEmoji + wish.Name,
         new QueryParameter(QueryParameterType.SetWishTo, wish.Id),
         new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
         QueryParameter.ReturnToFullList);
   }

   private void AddEditWishButton(BotUser user, int itemIndex, int pageIndex)
   {
      var wish = user.Wishes[itemIndex];

      const string pencilEmoji = "\u270f\ufe0f ";

      Keyboard.AddButton<EditWishQuery>(
         pencilEmoji + wish.Name,
         new QueryParameter(QueryParameterType.SetWishTo, wish.Id),
         new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
         QueryParameter.ReturnToFullList);
   }
}
