using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WishlistBot.Model.User;

/// <summary>
/// Набор кастомных эмодзи пользователя — миниатюры его вишей живут стикерами.
///
/// Набор создаёт бот (createNewStickerSet со sticker_type=custom_emoji), владелец — сам
/// пользователь, поэтому набор виден в его клиенте как обычный набор эмодзи. Править его
/// может только бот, которым он и создан. Одна строка на пользователя.
/// </summary>
public class WishEmojiPackModel
{
    [Key] public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public UserModel User { get; set; }

    /// <summary>
    /// Короткое имя набора в Telegram: <c>w&lt;хэш&gt;_by_&lt;bot_username&gt;</c>.
    /// null — набора ещё нет (ни одной миниатюры пользователь пока не получал).
    /// </summary>
    public string SetName { get; set; }

    /// <summary>Сколько стикеров в наборе по последним данным Telegram (лимит — 200).</summary>
    public int StickerCount { get; set; }

    /// <summary>
    /// Набор не находится в Telegram (удалён в клиенте или потерян): миниатюры не рисуем,
    /// пока суточная проверка не увидит набор снова.
    /// </summary>
    public bool IsBroken { get; set; }

    /// <summary>Когда последний раз проверяли набор через <c>getStickerSet</c> (UTC).</summary>
    public DateTime? LastCheckedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
