namespace EndlessBattle.Domain.Items;

public sealed record Recipe(
    string Id,
    string ResultItemId,
    int ResultQuantity,
    IReadOnlyList<(string ItemId, int Quantity)> Materials,
    int SuccessRate);
