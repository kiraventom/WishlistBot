using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

/// <summary>
/// Аватарка пользователя как кастомный эмодзи — в **отдельном** наборе, где всего один
/// стикер. Отдельный он потому, что аватарка одна и меняется независимо от вишей:
/// набор миниатюр (<see cref="WishEmojiPackModel"/>) живёт своей жизнью, а тут ровно
/// один стикер, который при смене аватарки заменяется.
/// </summary>
public class WishEmojiAvatarModel
{
    [Key] public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    /// <summary>Имя набора в Telegram: <c>a&lt;telegram_id в hex&gt;_by_&lt;bot_username&gt;</c>.</summary>
    public string SetName { get; set; }

    /// <summary>Id стикера-аватарки в наборе. null — аватарки нет, в списках ничего не рисуем.</summary>
    public string CustomEmojiId { get; set; }

    /// <summary>file_id стикера: <c>replaceStickerInSet</c>/<c>deleteStickerFromSet</c> берут только его.</summary>
    public string CustomEmojiFileId { get; set; }

    /// <summary>
    /// <c>file_unique_id</c> аватарки, из которой собран стикер: по нему понимаем, что
    /// аватарка не менялась и пересобирать нечего (file_id у неё каждый раз новый).
    /// </summary>
    public string SourceUniqueId { get; set; }

    /// <summary>Когда последний раз спрашивали Telegram про аватарку (UTC).</summary>
    public DateTime? CheckedAt { get; set; }

    /// <summary>Когда стикер-аватарка последний раз собирался (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
