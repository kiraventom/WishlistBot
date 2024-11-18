using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using WishlistBot.Database;
using WishlistBot.Actions;
using WishlistBot.Actions.Commands;
using WishlistBot.Queries;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.EditingWish;

namespace WishlistBot;

public class WishMessagesListener
{
   private readonly ILogger _logger;
   private readonly ITelegramBotClient _client;

   public WishMessagesListener(ILogger logger, ITelegramBotClient client)
   {
      _logger = logger;
      _client = client;
   }

   public async Task HandleWishMessageAsync(Message message, BotUser user)
   {
      switch (user.BotState)
      {
         case BotState.SettingWishName:
            await HandleSettingWishNameAsync(message, user);
            break;

         case BotState.SettingWishDescription:
            await HandleSettingWishDescriptionAsync(message, user);
            break;

         case BotState.SettingWishMedia:
            await HandleSettingWishMediaAsync(message, user);
            return;

         case BotState.SettingWishLinks:
            await HandleSettingWishLinksAsync(message, user);
            break;

         default:
            return;
      }

      await SendEditingWishMessageAsync(user);
   }

   private Task HandleSettingWishNameAsync(Message message, BotUser user)
   {
      var name = message.Text ?? message.Caption;
      if (name is null)
      {
         _logger.Warning("Received empty wish name, ignoring");
         return Task.CompletedTask;
      }

      user.CurrentWish.Name = name;
      _logger.Information("'{name}' is set as {firstName} [{userId}] wish name", name, user.FirstName, user.SenderId);
      return Task.CompletedTask;
   }

   private Task HandleSettingWishDescriptionAsync(Message message, BotUser user)
   {
      var description = message.Text ?? message.Caption;
      if (description is null)
      {
         _logger.Warning("Received empty wish description, ignoring");
         return Task.CompletedTask;
      }

      user.CurrentWish.Description = description;
      _logger.Information("'{description}' is set as {firstName} [{userId}] wish description", description, user.FirstName, user.SenderId);
      return Task.CompletedTask;
   }

   private async Task HandleSettingWishMediaAsync(Message message, BotUser user)
   {
      var fileId = message.Photo?.FirstOrDefault()?.FileId;
      if (fileId is null)
      {
         _logger.Warning("Received message with no photo, ignoring");
         return;
      }

      user.CurrentWish.FileId = fileId;
      _logger.Information("Photo [{fileId}] is added to {firstName} [{userId}] wish", fileId, user.FirstName, user.SenderId);

      if (message.MediaGroupId is not null)
         _logger.Warning("Media groups are not supported, ignoring other photos");

      await SendEditingWishMessageAsync(user);
   }

   private Task HandleSettingWishLinksAsync(Message message, BotUser user)
   {
      var text = message.Text ?? message.Caption;
      var linksEntities = message.Entities?.Where(e => e.Type == MessageEntityType.Url);
      if (text is null || linksEntities is null)
      {
         _logger.Warning("Received message without links, ignoring");
         return Task.CompletedTask;
      }

      var links = linksEntities.Select(l => text.Substring(l.Offset, l.Length));

      foreach (var link in links)
      {
         user.CurrentWish.Links.Add(link);
         _logger.Information("Link '{link}' is added to {firstName} [{userId}] wish", link, user.FirstName, user.SenderId);
      }

      return Task.CompletedTask;
   }

   private async Task SendEditingWishMessageAsync(BotUser user)
   {
      var message = new EditingWishMessage(_logger, user);
      await _client.SendOrEditBotMessage(_logger, user, message, forceNewMessage: true);
   }
}
