using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WishlistBot.BotMessages;
using WishlistBot.BotMessages.EditWish;
using WishlistBot.Media;
using WishlistBot.Model;
using WishlistBot.Model.User;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Listeners;

public class WishMessagesListener(ILogger logger, BotMessageSender sender) : IListener
{
    public async Task<HandleResult> HandleMessageAsync(Message message, UserContext userContext, int userId)
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

        // Черновик помнит, откуда он взялся: правка существующего виша или новый виш
        BotMessage wishDraftMessage = user.CurrentWish?.OriginalId is null
            ? new NewWishMessage(logger)
            : new EditWishMessage(logger);

        await sender.SendOrEdit(userContext, user.UserId, wishDraftMessage, forceNewMessage: true);
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
        // PhotoSize идут от меньшего к большему: берём самый крупный, из него и делаем блоб
        var photo = message.Photo?.LastOrDefault();
        if (photo is null)
        {
            logger.Warning("Received message with no photo, ignoring");
            return;
        }

        user.CurrentWish.MediaKey = await MediaStorage.Instance.RegisterAsync(photo);
        logger.Information("Photo [{key}] is added to {firstName} [{userId}] wish", user.CurrentWish.MediaKey, user.FirstName, user.UserId);

        if (message.MediaGroupId is not null)
            logger.Warning("Media groups are not supported, ignoring other photos");
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

        userContext.Entry(user.CurrentWish).Collection<LinkModel>(d => d.Links).Load();

        user.CurrentWish.Links.Clear();
        foreach (var link in links)
        {
            user.CurrentWish.Links.Add(new LinkModel() { Url = link });
            logger.Information("Link '{link}' is added to {firstName} [{userId}] wish", link, user.FirstName, user.UserId);
        }

        return Task.CompletedTask;
    }
}
