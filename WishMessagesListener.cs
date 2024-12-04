using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.EditWish;

namespace WishlistBot;

public class WishMessagesListener(ILogger logger, ITelegramBotClient client)
{
   public async Task HandleWishMessageAsync(Message message, BotUser user)
   {
      switch (user.BotState)
      {
         case BotState.ListenForWishName:
            await HandleSettingWishNameAsync(message, user);
            break;

         case BotState.ListenForWishDescription:
            await HandleSettingWishDescriptionAsync(message, user);
            break;

         case BotState.ListenForWishMedia:
            await HandleSettingWishMediaAsync(message, user);
            return;

         case BotState.ListenForWishLinks:
            await HandleSettingWishLinksAsync(message, user);
            break;

         default:
            return;
      }

      await SendEditWishMessageAsync(user);
   }

   private Task HandleSettingWishNameAsync(Message message, BotUser user)
   {
      var name = message.Text ?? message.Caption;
      if (name is null)
      {
         logger.Warning("Received empty wish name, ignoring");
         return Task.CompletedTask;
      }

      user.CurrentWish.Name = name;
      logger.Information("'{name}' is set as {firstName} [{userId}] wish name", name, user.FirstName, user.SenderId);
      return Task.CompletedTask;
   }

   private Task HandleSettingWishDescriptionAsync(Message message, BotUser user)
   {
      var description = message.Text ?? message.Caption;
      if (description is null)
      {
         logger.Warning("Received empty wish description, ignoring");
         return Task.CompletedTask;
      }

      user.CurrentWish.Description = description;
      logger.Information("'{description}' is set as {firstName} [{userId}] wish description", description, user.FirstName, user.SenderId);
      return Task.CompletedTask;
   }

   private async Task HandleSettingWishMediaAsync(Message message, BotUser user)
   {
      var fileId = message.Photo?.FirstOrDefault()?.FileId;
      if (fileId is null)
      {
         logger.Warning("Received message with no photo, ignoring");
         return;
      }

      user.CurrentWish.FileId = fileId;
      logger.Information("Photo [{fileId}] is added to {firstName} [{userId}] wish", fileId, user.FirstName, user.SenderId);

      if (message.MediaGroupId is not null)
         logger.Warning("Media groups are not supported, ignoring other photos");

      await SendEditWishMessageAsync(user);
   }

   private Task HandleSettingWishLinksAsync(Message message, BotUser user)
   {
      var text = message.Text ?? message.Caption;
      var linksEntities = message.Entities?.Where(e => e.Type == MessageEntityType.Url);
      if (text is null || linksEntities is null)
      {
         logger.Warning("Received message without links, ignoring");
         return Task.CompletedTask;
      }

      var links = linksEntities.Select(l => text.Substring(l.Offset, l.Length));

      foreach (var link in links)
      {
         user.CurrentWish.Links.Add(link);
         logger.Information("Link '{link}' is added to {firstName} [{userId}] wish", link, user.FirstName, user.SenderId);
      }

      return Task.CompletedTask;
   }

   private async Task SendEditWishMessageAsync(BotUser user)
   {
      var message = new EditWishMessage(logger);
      await client.SendOrEditBotMessage(logger, user, message, forceNewMessage: true);
   }
}
