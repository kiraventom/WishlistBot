using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Notification;

namespace WishlistBot.BotMessages.EditWish;

public class FinishEditWishMessage : BotMessage
{
   private readonly UsersDb _usersDb;

   public FinishEditWishMessage(ILogger logger, UsersDb usersDb) : base(logger)
   {
      _usersDb = usersDb;
   }

#pragma warning disable CS1998
   protected override async Task InitInternal(BotUser user, QueryParameterCollection parameters)
   {
      Keyboard = new BotKeyboard(parameters)
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow();

      if (parameters.Pop(QueryParameterType.ReturnToFullList))
         Keyboard.AddButton<FullListQuery>("Назад к списку");
      else
         Keyboard.AddButton<CompactListQuery>("Назад к моим вишам");

      if (!user.Wishes.Contains(user.CurrentWish))
      {
         user.Wishes.Add(user.CurrentWish);
         Text.Italic("Виш добавлен!");

         var newWish = user.CurrentWish;

         var subscribers = _usersDb.Values.Values
            .Where(u => u.Subscriptions.Contains(user.SubscribeId));
         
         var newWishNotification = new NewWishNotificationMessage(Logger, user, newWish);
         await NotificationService.Instance.Send(newWishNotification, subscribers);
      }
      else
      {
         Text.Italic("Виш изменён!");

         var editedWish = user.CurrentWish;

         var subscribers = _usersDb.Values.Values
            .Where(u => u.Subscriptions.Contains(user.SubscribeId));
         
         // TODO Track what wish property changed
         var editWishNotification = new EditWishNotificationMessage(Logger, user, editedWish, WishPropertyType.Name);
         await NotificationService.Instance.Send(editWishNotification, subscribers);
      }

      user.CurrentWish = null;
   }
}
