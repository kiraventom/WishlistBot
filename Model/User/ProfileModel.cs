using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishlistBot.Model.User;

public class ProfileModel
{
    [Key] public int ProfileId { get; set; }

    public int UserId { get; set; }
    public UserModel User { get; set; }

    // Дата рождения тремя колонками: год может быть скрыт (Telegram его отдаёт по желанию),
    // а без года «10 марта» — всё ещё нормальная дата рождения. День и месяц либо есть
    // вместе, либо нет вместе — за этим следит BirthdayUtils.ToBirthday.
    public int? BirthdayDay { get; set; }
    public int? BirthdayMonth { get; set; }
    public int? BirthdayYear { get; set; }

    /// <summary>
    /// Дата рождения одним объектом, чтобы экраны не собирали её из трёх полей.
    /// Мусор в колонках (несуществующий день, год из будущего) читается как «не указана».
    /// </summary>
    [NotMapped]
    public Birthday? Birthday
    {
        get => BirthdayUtils.ToBirthday(BirthdayDay, BirthdayMonth, BirthdayYear);
        set
        {
            BirthdayDay = value?.Day;
            BirthdayMonth = value?.Month;
            BirthdayYear = value?.Year;
        }
    }

    public string Notes { get; set; }
}
