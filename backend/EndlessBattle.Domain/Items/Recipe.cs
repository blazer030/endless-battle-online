namespace EndlessBattle.Domain.Items;

public sealed record Recipe(
    string Id,
    string ResultItemId,
    int ResultQuantity,
    IReadOnlyList<(string ItemId, int Quantity)> Materials,
    int SuccessRate)
{
    // 要先完成這個任務才能合成；原版只要接過，本作要完成
    public string? RequiredMissionId { get; init; }
}
