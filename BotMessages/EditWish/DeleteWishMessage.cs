using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Database.Users;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages.EditWish;

public class DeleteWishMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public DeleteWishMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters);

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<CompactListQuery>("Назад к моим вишам");

      parameters.Peek(QueryParameterType.SetCurrentWishTo, out var wishIndex);

      var deletedWish = user.Wishes[(int)wishIndex];
      user.Wishes.Remove(deletedWish);
      user.CurrentWish = null;

      Text.Italic("Виш удалён!");

      // TODO DRY
      var subscribers = _usersDb.Values.Values
         .Where(u => u.Subscriptions.Contains(user.SubscribeId));
      var deleteWishNotification = new DeleteWishNotificationMessage(Logger, user, deletedWish);
      await NotificationService.Instance.Send(deleteWishNotification, subscribers);
   }
}
