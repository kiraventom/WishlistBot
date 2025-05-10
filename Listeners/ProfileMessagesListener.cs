using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;
using WishlistBot.BotMessages.Profile;
using System.Globalization;

namespace WishlistBot.Listeners;

public class ProfileMessagesListener(ILogger logger, ITelegramBotClient client) : IListener
{
    private readonly CultureInfo _ruCulture = new CultureInfo("ru-RU");

    public async Task<bool> HandleMessageAsync(Message message, UserContext userContext, int userId)
    {
        var user = userContext.Users.Include(u => u.Profile).First(u => u.UserId == userId);
        switch (user.BotState)
        {
            case BotState.ListenForBirthday:
                await HandleSettingProfileBirthday(message, userContext, user);
                break;

            case BotState.ListerForProfileNotes:
                await HandleSettingProfileNotes(message, userContext, user);
                break;

            default:
                return false;
        }

        var editProfileMessage = new EditProfileMessage(logger);
        await client.SendOrEditBotMessage(logger, userContext, user.UserId, editProfileMessage, forceNewMessage: true);
        return true;
    }

    private Task HandleSettingProfileBirthday(Message message, UserContext userContext, UserModel user)
    {
        var birthdayStr = message.Text ?? message.Caption;
        if (birthdayStr is null)
        {
            logger.Warning("Received empty birthday string, ignoring");
            return Task.CompletedTask;
        }

        var didParse = DateOnly.TryParse(birthdayStr, _ruCulture, out var birthday);
        if (!didParse)
        {
            // Received invalid input, ignoring
            return Task.CompletedTask;
        }

        user.Profile.Birthday = birthday;

        logger.Information("'{birthday}' is set as {firstName} [{userId}] birthday", birthday.ToString("dd.MM/yyyy"), user.FirstName, user.UserId);
        return Task.CompletedTask;
    }

    private Task HandleSettingProfileNotes(Message message, UserContext userContext, UserModel user)
    {
        var notes = message.Text ?? message.Caption;
        if (notes is null)
        {
            logger.Warning("Received empty notes, ignoring");
            return Task.CompletedTask;
        }

        user.Profile.Notes = notes;
        logger.Information("'{notes}' is set as {firstName} [{userId}] profile notes", notes, user.FirstName, user.UserId);
        return Task.CompletedTask;
    }
}

