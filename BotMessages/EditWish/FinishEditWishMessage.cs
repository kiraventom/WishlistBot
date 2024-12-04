using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.Parameters;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;

namespace WishlistBot.BotMessages.EditWish;

public class FinishEditWishMessage : BotMessage
{
   public FinishEditWishMessage(ILogger logger) : base(logger)
   {
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
      }
      else
      {
         Text.Italic("Виш изменён!");
      }

      user.CurrentWish = null;
   }
}
