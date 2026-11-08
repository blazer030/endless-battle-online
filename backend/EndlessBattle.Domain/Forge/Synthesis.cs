using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Forge;

// MaterialsConsumed：依配方合併同種材料後的總消耗；Cost 只算成功的次數
public sealed record SynthesisOutcome(int Successes, IReadOnlyList<(string ItemId, int Quantity)> MaterialsConsumed, long Cost);

public static class Synthesis
{
    public const long CostPerAttempt = 1000;

    // 原因代碼依序為 missionIncomplete、invalidQuantity、missingMaterials、notEnoughMoney、bagFull，只回第一個。
    // bag 是整個背包（不含已裝備的列）。金錢與背包都以全部成功估算，避免擲完才發現付不出或放不下
    public static IReadOnlyList<string> Check(
        Recipe recipe, int attempts, IReadOnlyList<InventoryEntry> bag, long money, int bagBonus, Func<string, ItemDefinition> getItem,
        IReadOnlySet<string>? completedMissionIds = null)
    {
        if (recipe.RequiredMissionId is { } missionId && completedMissionIds?.Contains(missionId) != true)
        {
            return ["missionIncomplete"];
        }

        var materials = CheckMaterials(recipe, attempts, bag);
        if (materials.Count > 0)
        {
            return materials;
        }

        if (CostPerAttempt * attempts > money)
        {
            return ["notEnoughMoney"];
        }

        var result = getItem(recipe.ResultItemId);
        var sameCategory = AfterTaking(bag, Consumed(recipe, attempts)).Where(entry => getItem(entry.ItemId).Category == result.Category).ToList();
        return InventoryRules.TryAdd(sameCategory, result, recipe.ResultQuantity * attempts, bagBonus).Discarded > 0 ? ["bagFull"] : [];
    }

    // 每次獨立擲 rand(1, 100) ≤ 成功率（WOG syn.php:426-433）；材料一律依次數全扣
    public static SynthesisOutcome Attempt(Recipe recipe, int attempts, IReadOnlyList<InventoryEntry> bag, IRandomSource random)
    {
        ForgeGuard.Ensure(CheckMaterials(recipe, attempts, bag));
        var successes = 0;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (PhpRandom.Between(random, 1, 100) <= recipe.SuccessRate)
            {
                successes++;
            }
        }

        return new SynthesisOutcome(successes, Consumed(recipe, attempts), CostPerAttempt * successes);
    }

    // 依背包順序從各格扣掉材料，回傳數量有變的格子（Quantity 為 0 表示整格用完）；呼叫端應先確認材料足夠
    public static IReadOnlyList<InventoryEntry> TakeMaterials(IReadOnlyList<InventoryEntry> bag, IReadOnlyList<(string ItemId, int Quantity)> materials)
    {
        var changed = new List<InventoryEntry>();
        foreach (var (itemId, quantity) in materials)
        {
            var remaining = quantity;
            foreach (var entry in bag.Where(entry => entry.ItemId == itemId))
            {
                if (remaining == 0)
                {
                    break;
                }

                var taken = Math.Min(remaining, entry.Quantity);
                changed.Add(entry with { Quantity = entry.Quantity - taken });
                remaining -= taken;
            }

            if (remaining > 0)
            {
                throw new InvalidOperationException($"材料不足：{itemId}");
            }
        }

        return changed;
    }

    // 以 long 計算，次數極大時不會溢位
    private static IReadOnlyList<string> CheckMaterials(Recipe recipe, int attempts, IReadOnlyList<InventoryEntry> bag)
    {
        if (attempts < 1)
        {
            return ["invalidQuantity"];
        }

        var missing = Required(recipe).Any(material =>
            (long)material.Quantity * attempts > bag.Where(entry => entry.ItemId == material.ItemId).Sum(entry => (long)entry.Quantity));
        return missing ? ["missingMaterials"] : [];
    }

    private static List<(string ItemId, int Quantity)> Required(Recipe recipe) =>
        recipe.Materials
            .GroupBy(material => material.ItemId)
            .Select(group => (group.Key, group.Sum(material => material.Quantity)))
            .ToList();

    private static List<(string ItemId, int Quantity)> Consumed(Recipe recipe, int attempts) =>
        Required(recipe).Select(material => (material.ItemId, material.Quantity * attempts)).ToList();

    // 扣掉材料後的背包（扣光的格子移除）
    public static IReadOnlyList<InventoryEntry> AfterTaking(IReadOnlyList<InventoryEntry> bag, IReadOnlyList<(string ItemId, int Quantity)> materials)
    {
        var changed = TakeMaterials(bag, materials).ToDictionary(entry => entry.Id);
        return bag.Select(entry => changed.GetValueOrDefault(entry.Id, entry)).Where(entry => entry.Quantity > 0).ToList();
    }
}
