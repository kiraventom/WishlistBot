namespace WishlistBot.Model.User;

// Общий вид «страницы списка» — чтобы одна и та же утилита листания работала
// и для списков на пользователя (ListPositionModel), и для вишлиста, у которого
// страница живёт в настройках просмотра пары «зритель → чей вишлист».
public interface IListPage
{
    int Page { get; set; }
}
