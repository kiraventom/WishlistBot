namespace WishlistBot.Model.User;

/// <summary>
/// Где виш находится в списке, который видит зритель: сам список в порядке отображения
/// и индекс виша в нём (<see cref="Index"/> == -1, если виша в списке нет).
/// </summary>
public sealed record WishListLocation(IReadOnlyList<WishModel> Wishes, int Index);
