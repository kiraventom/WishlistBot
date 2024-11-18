using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;

namespace WishlistBot.BotMessages;

public class FinishedWishEditingMessage : BotMessage
{
   public FinishedWishEditingMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user)
   {
      Keyboard = new BotKeyboard()
         .AddButton<SetWishNameQuery>("Добавить ещё виш")
         .NewRow()
         .AddButton<MyWishesQuery>("Назад к моим вишам");

      Text = "Виш добавлен!";

      user.BotState = BotState.WishAdded;
      user.Wishes.Add(user.CurrentWish);
      user.CurrentWish = null;
   }
}
