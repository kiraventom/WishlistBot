using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;
using System.Text;

namespace WishlistBot.BotMessages.EditingWish;

public class SetNewWishNameMessage : SetWishNameMessage
{
   public SetNewWishNameMessage(ILogger logger, BotUser user) : base(logger, user)
   {
      user.CurrentWish = new Wish();
      Text = "Укажите краткое название виша";
   }
}
