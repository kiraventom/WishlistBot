using Serilog;
using WishlistBot.Queries;
using WishlistBot.Queries.EditWish;
using WishlistBot.Database.Users;
using WishlistBot.BotMessages.Notification;
using WishlistBot.Notification;
using WishlistBot.QueryParameters;
using WishlistBot.Model;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.BotMessages.EditWish;

[ChildMessage(typeof(EditWishMessage))]
[AllowedTypes(QueryParameterType.ReturnToFullList, QueryParameterType.SetWishTo)]
public class FinishEditWishMessage(ILogger logger) : BotMessage(logger)
{
    protected override async Task InitInternal(UserContext userContext, int userId, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<SetWishNameQuery>("Добавить ещё виш")
           .NewRow();

        if (parameters.Pop(QueryParameterType.SetWishTo, out var wishId))
            await EditWish(userContext, userId, wishId);
        else
            await AddWish(userContext, userId);

        if (parameters.Pop(QueryParameterType.ReturnToFullList))
            Keyboard.NewRow().AddButton<FullListQuery>("Назад к списку");
        else
            Keyboard.NewRow().AddButton<CompactListQuery>("Назад к моим вишам");

        var user = userContext.Users.Include(u => u.CurrentWish).First(u => u.UserId == userId);
        userContext.WishDrafts.Remove(user.CurrentWish);
        user.CurrentWish = null;
    }

    protected override async Task Legacy_InitInternal(BotUser user, QueryParameterCollection parameters)
    {
        Keyboard
           .AddButton<SetWishNameQuery>("Добавить ещё виш")
           .NewRow();

        if (parameters.Pop(QueryParameterType.SetWishTo, out var wishId))
            await Legacy_EditWish(user, wishId);
        else
            await Legacy_AddWish(user);

        if (parameters.Pop(QueryParameterType.ReturnToFullList))
            Keyboard.NewRow().AddButton<FullListQuery>("Назад к списку");
        else
            Keyboard.NewRow().AddButton<CompactListQuery>("Назад к моим вишам");

        user.CurrentWish = null;
    }

    private async Task AddWish(UserContext userContext, int userId)
    {
        var user = userContext.Users.Include(u => u.Wishes).First(u => u.UserId == userId);
        var newWishDraft = user.CurrentWish;

        if (newWishDraft is null)
        {
            Logger.Error("[{uId}]: New wish is null", user.UserId);
            throw new NotSupportedException("Current wish is null");
        }

        // TODO: Combine code of creating wish from draft
        var newWish = new WishModel()
        {
            ClaimerId = newWishDraft.ClaimerId,
            OwnerId = newWishDraft.OwnerId,
            Name = newWishDraft.Name,
            Description = newWishDraft.Description,
            FileId = newWishDraft.FileId,
            PriceRange = newWishDraft.PriceRange,
        };

        foreach (var draftLink in newWishDraft.Links)
        {
            var link = new LinkModel()
            {
                Url = draftLink.Url
            };

            newWish.Links.Add(link);
        }

        using (var transaction = userContext.Database.BeginTransaction())
        {
            try
            {
                user.Wishes.Add(newWish);
                userContext.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        Text.Italic("Виш добавлен!");

        var wishIndex = user.Wishes.IndexOf(newWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<EditWishQuery>("Изменить виш",
                                     new QueryParameter(QueryParameterType.SetWishTo, newWish.WishId),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
                                     QueryParameter.ReturnToFullList);

        var newWishNotification = new NewWishNotificationMessage(Logger, user.UserId, newWish.WishId);
        await NotificationService.Instance.SendToSubscribers(newWishNotification, userContext, user.UserId);
    }

    private async Task Legacy_AddWish(BotUser user)
    {
        var newWish = user.CurrentWish;

        if (newWish is null)
        {
            Logger.Error("[{uId}]: New wish is null", user.SenderId);
            throw new NotSupportedException("Current wish is null");
        }

        user.Wishes.Add(newWish);
        Text.Italic("Виш добавлен!");

        var wishIndex = user.Wishes.IndexOf(newWish);
        var pageIndex = wishIndex / ListMessageUtils.ItemsPerPage;

        Keyboard
           .AddButton<EditWishQuery>("Изменить виш",
                                     new QueryParameter(QueryParameterType.SetWishTo, newWish.Id),
                                     new QueryParameter(QueryParameterType.SetListPageTo, pageIndex),
                                     QueryParameter.ReturnToFullList);

        var newWishNotification = new NewWishNotificationMessage(Logger, user, newWish);
        await NotificationService.Instance.Legacy_SendToSubscribers(newWishNotification, user);
    }

    private async Task EditWish(UserContext userContext, int userId, long wishId)
    {
        var user = userContext.Users
            .Include(u => u.CurrentWish)
            .ThenInclude(u => u.Links)
            .Include(u => u.Wishes)
            .ThenInclude(u => u.Links)
            .First(u => u.UserId == userId);

        var editedWishDraft = user.CurrentWish;

        var editedWish = new WishModel()
        {
            ClaimerId = editedWishDraft.ClaimerId,
            OwnerId = editedWishDraft.OwnerId,
            Name = editedWishDraft.Name,
            Description = editedWishDraft.Description,
            FileId = editedWishDraft.FileId,
            PriceRange = editedWishDraft.PriceRange,
        };

        foreach (var draftLink in editedWishDraft.Links)
        {
            var link = new LinkModel()
            {
                Url = draftLink.Url
            };

            editedWish.Links.Add(link);
        }

        var wishBeforeEditing = user.Wishes.FirstOrDefault(w => w.WishId == wishId);
        if (wishBeforeEditing is null)
        {
            Logger.Error("Can't find wish {id} to remove after editing", wishId);

            if (editedWishDraft is null)
            {
                Logger.Error("[{uId}]: Edited wish is null", user.UserId);
                throw new NotSupportedException("Edited wish is null");
            }

            using (var transaction = userContext.Database.BeginTransaction())
            {
                try
                {
                    userContext.Wishes.Add(editedWish);
                    userContext.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            Text.Italic("Ваши изменения сохранены, но произошла ошибка. Сообщите разработчику об этом");
        }
        else
        {
            Text.Italic("Виш изменён!");

            using (var transaction = userContext.Database.BeginTransaction())
            {
                try
                {
                    var wishIndex = user.Wishes.IndexOf(wishBeforeEditing);
                    user.Wishes.Remove(wishBeforeEditing);
                    user.Wishes.Insert(wishIndex, editedWish);

                    userContext.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            var wishPropertyType = WishPropertyType.None;

            if (wishBeforeEditing.Name != editedWishDraft.Name)
                wishPropertyType |= WishPropertyType.Name;

            if (wishBeforeEditing.Description != editedWishDraft.Description)
                wishPropertyType |= WishPropertyType.Description;

            if (!wishBeforeEditing.Links.SequenceEqual(editedWishDraft.Links))
                wishPropertyType |= WishPropertyType.Links;

            if (wishBeforeEditing.FileId != editedWishDraft.FileId)
                wishPropertyType |= WishPropertyType.Media;

            if (wishPropertyType != WishPropertyType.None)
            {
                var editWishNotification = new EditWishNotificationMessage(Logger, user.UserId, editedWish.WishId, wishPropertyType);
                await NotificationService.Instance.SendToSubscribers(editWishNotification, userContext, user.UserId);
            }
        }
    }

    private async Task Legacy_EditWish(BotUser user, long wishId)
    {
        var editedWish = user.CurrentWish;
        var wishBeforeEditing = user.Wishes.FirstOrDefault(w => w.Id == wishId);
        if (wishBeforeEditing is null)
        {
            Logger.Error("Can't find wish {id} to remove after editing", wishId);

            if (editedWish is null)
            {
                Logger.Error("[{uId}]: Edited wish is null", user.SenderId);
                throw new NotSupportedException("Edited wish is null");
            }

            user.Wishes.Add(editedWish);
            Text.Italic("Ваши изменения сохранены, но произошла ошибка. Сообщите разработчику об этом");
            return;
        }

        Text.Italic("Виш изменён!");

        var wishIndex = user.Wishes.IndexOf(wishBeforeEditing);
        user.Wishes.Remove(wishBeforeEditing);
        user.Wishes.Insert(wishIndex, editedWish);

        var wishPropertyType = WishPropertyType.None;

        if (wishBeforeEditing.Name != editedWish.Name)
            wishPropertyType |= WishPropertyType.Name;

        if (wishBeforeEditing.Description != editedWish.Description)
            wishPropertyType |= WishPropertyType.Description;

        if (!wishBeforeEditing.Links.SequenceEqual(editedWish.Links))
            wishPropertyType |= WishPropertyType.Links;

        if (wishBeforeEditing.FileId != editedWish.FileId)
            wishPropertyType |= WishPropertyType.Media;

        if (wishPropertyType != WishPropertyType.None)
        {
            var editWishNotification = new EditWishNotificationMessage(Logger, user, editedWish, wishPropertyType);
            await NotificationService.Instance.Legacy_SendToSubscribers(editWishNotification, user);
        }
    }
}
