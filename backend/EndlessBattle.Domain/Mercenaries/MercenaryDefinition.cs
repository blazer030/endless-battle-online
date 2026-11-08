namespace EndlessBattle.Domain.Mercenaries;

// Price 為 null：只能在勳章所兌換；百分比都是 1～100
public sealed record MercenaryDefinition(
    string Id, string Name, long? Price, int Fights, int MoneyPercent, int DropPercent, int ExpPercent, int DeathCost);
