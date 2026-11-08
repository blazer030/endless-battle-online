using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Domain.Honour;

// ItemId 與 MercenaryId 恰好一個非 null；兌換傭兵時 Quantity 為 1；Cost 只能是勳章
public sealed record HonourExchange(string Id, string? ItemId, string? MercenaryId, int Quantity, IReadOnlyList<ItemQuantity> Cost);
