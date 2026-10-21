namespace EndlessBattle.Domain.Content;

// 欄位對應 WOG 怪物資料：Element 0 為無屬性、1～6 對應六屬性；Meet 為遇敵率（0～100）
// PlaceId 0 為全域怪；SubPlaces 為所屬子地區（1～4），全域怪與無子地區的地區為空
public sealed record MonsterDefinition(
    string Id,
    string Name,
    int Level,
    int Hp,
    int Sp,
    int At,
    int Mat,
    int Df,
    int Mdf,
    int Agi,
    int Au,
    int Element,
    long JobExp,
    int Meet,
    int PlaceId = 0,
    IReadOnlyList<int>? SubPlaces = null)
{
    public IReadOnlyList<int> SubPlaces { get; init; } = SubPlaces ?? [];
}
