using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WishlistBot.BotMessages.EditWish;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Listeners;

public class WishMessagesListener(ILogger logger, ITelegramBotClient client) : IListener
{
    public async Task<bool> HandleMessageAsync(Message message, UserContext userContext, int userId)
    {
        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);
        switch (user.BotState)
        {
            case BotState.ListenForWishName:
                await HandleSettingWishNameAsync(message, userContext, user);
                break;

            case BotState.ListenForWishDescription:
                await HandleSettingWishDescriptionAsync(message, userContext, user);
                break;

            case BotState.ListenForWishMedia:
                await HandleSettingWishMediaAsync(message, userContext, user);
                break;

            case BotState.ListenForWishLinks:
                await HandleSettingWishLinksAsync(message, userContext, user);
                break;

            default:
                return false;
        }

        await SendEditWishMessageAsync(userContext, user);
        return true;
    }

    private Task HandleSettingWishNameAsync(Message message, UserContext userContext, UserModel user)
    {
        var name = message.Text ?? message.Caption;
        if (name is null)
        {
            logger.Warning("Received empty wish name, ignoring");
            return Task.CompletedTask;
        }

        if (user.CurrentWish is null)
        {
            user.CurrentWish = new WishDraftModel() { Name = name };
        }
        else
        {
            user.CurrentWish.Name = name;
        }

        logger.Information("'{name}' is set as {firstName} [{userId}] wish name", name, user.FirstName, user.UserId);
        return Task.CompletedTask;
    }

    private Task HandleSettingWishDescriptionAsync(Message message, UserContext userContext, UserModel user)
    {
        var description = message.Text ?? message.Caption;
        if (description is null)
        {
            logger.Warning("Received empty wish description, ignoring");
            return Task.CompletedTask;
        }

        user.CurrentWish.Description = description;
        logger.Information("'{description}' is set as {firstName} [{userId}] wish description", description, user.FirstName, user.UserId);
        return Task.CompletedTask;
    }

    private async Task HandleSettingWishMediaAsync(Message message, UserContext userContext, UserModel user)
    {
        var fileId = message.Photo?.FirstOrDefault()?.FileId;
        if (fileId is null)
        {
            logger.Warning("Received message with no photo, ignoring");
            return;
        }

        user.CurrentWish.FileId = fileId;
        logger.Information("Photo [{fileId}] is added to {firstName} [{userId}] wish", fileId, user.FirstName, user.UserId);

        if (message.MediaGroupId is not null)
            logger.Warning("Media groups are not supported, ignoring other photos");

        await SendEditWishMessageAsync(userContext, user);
    }

    private Task HandleSettingWishLinksAsync(Message message, UserContext userContext, UserModel user)
    {
        var text = message.Text ?? message.Caption;
        var linksEntities = message.Entities?.Where(e => e.Type == MessageEntityType.Url);
        if (text is null || linksEntities is null)
        {
            logger.Warning("Received message without links, ignoring");
            return Task.CompletedTask;
        }

        var links = linksEntities.Select(l => text.Substring(l.Offset, l.Length));

        userContext.Entry<WishDraftModel>(user.CurrentWish).Collection<LinkModel>(d => d.Links).Load();

        user.CurrentWish.Links.Clear();
        foreach (var link in links)
        {
            user.CurrentWish.Links.Add(new LinkModel() { Url = link });
            logger.Information("Link '{link}' is added to {firstName} [{userId}] wish", link, user.FirstName, user.UserId);
        }

        return Task.CompletedTask;
    }

    private async Task SendEditWishMessageAsync(UserContext userContext, UserModel userModel)
    {
        var message = new EditWishMessage(logger);
        await client.SendOrEditBotMessage(logger, userContext, userModel.UserId, message, forceNewMessage: true);
    }
}
