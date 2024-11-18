using Serilog;
using WishlistBot.Keyboard;
using WishlistBot.Queries;
using WishlistBot.Queries.EditingWish;
using WishlistBot.Database;
using WishlistBot.BotMessages.EditingWish;

namespace WishlistBot.BotMessages;

public class ClearWishMediaMessage : EditingWishMessage
{
   public ClearWishMediaMessage(ILogger logger) : base(logger)
   {
   }

   protected override void InitInternal(BotUser user)
   {
      PhotoFileId = null;
      user.CurrentWish.FileId = null;
   }
}
