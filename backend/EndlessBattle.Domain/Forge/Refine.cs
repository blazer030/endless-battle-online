using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Forge;

// ConsumedStone／ConsumedSafeStone：這次要扣掉的精鍊石與安全精鍊石（各 1 顆）
public sealed record RefineOutcome(bool Success, InventoryEntry Entry, bool ConsumedStone, bool ConsumedSafeStone);

public static class Refine
{
    // 原因代碼依序為 notEquipment、notRefineStone、wrongStoneLevel、atRefineMax（WOG 先比等級再比上限），只回第一個
    public static IReadOnlyList<string> Check(InventoryEntry equipment, ItemDefinition equipmentDef, ItemDefinition stone)
    {
        if (!equipmentDef.IsEquipment)
        {
            return ["notEquipment"];
        }

        if (stone.Refine is null)
        {
            return ["notRefineStone"];
        }

        var target = equipment.RefineLevel + 1;
        if (stone.Refine.Level != target)
        {
            return ["wrongStoneLevel"];
        }

        return target > equipmentDef.RefineMax ? ["atRefineMax"] : [];
    }

    // 擲 r = rand(1, 100)，10n > r 失敗（成功率 101 − 10n %）；成功才依序擲各能力
    public static RefineOutcome Attempt(
        InventoryEntry equipment, ItemDefinition equipmentDef, ItemDefinition stone, bool useSafeStone, IRandomSource random)
    {
        ForgeGuard.Ensure(Check(equipment, equipmentDef, stone));
        var spec = stone.Refine ?? throw new InvalidOperationException("Check 已確認是精鍊石");
        var target = equipment.RefineLevel + 1;
        var roll = PhpRandom.Between(random, 1, 100);
        if (10 * target > roll)
        {
            // 失敗只損石頭：用安全精鍊石時改扣安全精鍊石、保留精鍊石；裝備永不損毀
            return new RefineOutcome(false, equipment, ConsumedStone: !useSafeStone, ConsumedSafeStone: useSafeStone);
        }

        var bonus = equipment.RefineBonus + Roll(spec, random);
        return new RefineOutcome(true, equipment with { RefineLevel = target, RefineBonus = bonus }, ConsumedStone: true, ConsumedSafeStone: false);
    }

    // 依 StatBonus 的欄位順序擲骰（引數由左至右求值），沒有範圍的能力不擲
    private static StatBonus Roll(RefineStoneSpec spec, IRandomSource random) => new(
        Roll(spec.Str, random),
        Roll(spec.Smart, random),
        Roll(spec.Life, random),
        Roll(spec.Vit, random),
        Roll(spec.Agi, random),
        Roll(spec.Au, random),
        Roll(spec.Be, random),
        Roll(spec.At, random),
        Roll(spec.Mat, random),
        Roll(spec.Df, random),
        Roll(spec.Mdf, random));

    private static int Roll(StatRange? range, IRandomSource random) =>
        range is null ? 0 : PhpRandom.Between(random, range.Minimum, range.Maximum);
}
