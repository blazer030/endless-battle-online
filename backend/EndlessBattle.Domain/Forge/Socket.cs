using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Forge;

public sealed record SocketOutcome(InventoryEntry Entry, SocketedStone Socket);

// Recovered 為 true 時 StoneId 的魔石回到背包，否則消失
public sealed record RemoveOutcome(bool Recovered, InventoryEntry Entry, string StoneId);

public sealed record DrillOutcome(bool Success, InventoryEntry Entry);

// 孔位從 1 起算；魔石種類 = 魔石的道具 id
public static class Socket
{
    public const int MaxHoles = 4;

    // 原因代碼依序為 notEquipment、notStone、holeOutOfRange、holeOccupied、duplicateStone，只回第一個
    public static IReadOnlyList<string> CheckInsert(InventoryEntry equipment, ItemDefinition equipmentDef, int hole, ItemDefinition stone) =>
        equipmentDef.IsEquipment ? InsertReasons(equipment, hole, stone) : ["notEquipment"];

    public static SocketOutcome Insert(InventoryEntry equipment, int hole, ItemDefinition stone, IRandomSource random)
    {
        ForgeGuard.Ensure(InsertReasons(equipment, hole, stone));
        var spec = stone.Stone!;
        var socket = new SocketedStone(hole, stone.Id, spec.Random ? Roll(spec.Min ?? StatBonus.Zero, spec.Max ?? StatBonus.Zero, random) : spec.Fixed);
        return new SocketOutcome(equipment with { Sockets = [.. equipment.Sockets.Append(socket).OrderBy(other => other.Hole)] }, socket);
    }

    // 原因代碼依序為 notEquipment、holeOutOfRange、holeEmpty，只回第一個
    public static IReadOnlyList<string> CheckRemove(InventoryEntry equipment, ItemDefinition equipmentDef, int hole) =>
        equipmentDef.IsEquipment ? RemoveReasons(equipment, hole) : ["notEquipment"];

    // 擲 rand(1, 100) < 25 回收（24%）；不論成敗該孔都清空
    public static RemoveOutcome Remove(InventoryEntry equipment, int hole, IRandomSource random)
    {
        ForgeGuard.Ensure(RemoveReasons(equipment, hole));
        var removed = equipment.Sockets.Single(socket => socket.Hole == hole);
        var recovered = PhpRandom.Between(random, 1, 100) < 25;
        return new RemoveOutcome(recovered, equipment with { Sockets = [.. equipment.Sockets.Where(socket => socket.Hole != hole)] }, removed.StoneId);
    }

    // 原因代碼依序為 notEquipment、maxHoles，只回第一個
    public static IReadOnlyList<string> CheckDrill(InventoryEntry equipment, ItemDefinition equipmentDef)
    {
        if (!equipmentDef.IsEquipment)
        {
            return ["notEquipment"];
        }

        return equipment.Holes >= MaxHoles ? ["maxHoles"] : [];
    }

    // 擲 rand(1, 100) ≤ 8 成功：同一筆道具的孔數 +1（不換道具 id）
    public static DrillOutcome Drill(InventoryEntry equipment, ItemDefinition equipmentDef, IRandomSource random)
    {
        ForgeGuard.Ensure(CheckDrill(equipment, equipmentDef));
        return PhpRandom.Between(random, 1, 100) <= 8
            ? new DrillOutcome(true, equipment with { Holes = equipment.Holes + 1 })
            : new DrillOutcome(false, equipment);
    }

    private static IReadOnlyList<string> RemoveReasons(InventoryEntry equipment, int hole)
    {
        if (!InRange(equipment, hole))
        {
            return ["holeOutOfRange"];
        }

        return equipment.Sockets.Any(socket => socket.Hole == hole) ? [] : ["holeEmpty"];
    }

    private static bool InRange(InventoryEntry equipment, int hole) => hole >= 1 && hole <= Math.Min(equipment.Holes, MaxHoles);

    // 固定魔石不擲骰；隨機魔石依 StatBonus 的欄位順序（Str…Be、At、Mat、Df、Mdf、HpMax）各擲一次，範圍 [0, 0] 也照擲
    private static StatBonus Roll(StatBonus min, StatBonus max, IRandomSource random) => new(
        PhpRandom.Between(random, min.Str, max.Str),
        PhpRandom.Between(random, min.Smart, max.Smart),
        PhpRandom.Between(random, min.Life, max.Life),
        PhpRandom.Between(random, min.Vit, max.Vit),
        PhpRandom.Between(random, min.Agi, max.Agi),
        PhpRandom.Between(random, min.Au, max.Au),
        PhpRandom.Between(random, min.Be, max.Be),
        PhpRandom.Between(random, min.At, max.At),
        PhpRandom.Between(random, min.Mat, max.Mat),
        PhpRandom.Between(random, min.Df, max.Df),
        PhpRandom.Between(random, min.Mdf, max.Mdf),
        PhpRandom.Between(random, min.HpMax, max.HpMax));

    private static IReadOnlyList<string> InsertReasons(InventoryEntry equipment, int hole, ItemDefinition stone)
    {
        if (stone.Stone is null)
        {
            return ["notStone"];
        }

        if (!InRange(equipment, hole))
        {
            return ["holeOutOfRange"];
        }

        if (equipment.Sockets.Any(socket => socket.Hole == hole))
        {
            return ["holeOccupied"];
        }

        return equipment.Sockets.Any(socket => socket.StoneId == stone.Id) ? ["duplicateStone"] : [];
    }
}
