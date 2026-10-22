using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Domain.Honour;

// Reasons 非空時其餘欄位不可用；Item 是放進付款後背包的結果
public sealed record HonourOutcome(DeliveryPlan Payment, AddResult? Item, MercenaryContract? Mercenary, IReadOnlyList<string> Reasons);

// 勳章所（WOG class/wog_act_honor.php）：一次兌換一筆，勳章從背包扣，取用順序同任務交付
public static class HonourShop
{
    public static bool Affordable(HonourExchange exchange, IReadOnlyDictionary<string, int> owned) =>
        exchange.Cost.All(cost => owned.GetValueOrDefault(cost.ItemId) >= cost.Quantity);

    // 原因依序 honourUnknown（單獨回傳）、medalsMissing、mercenaryHired、bagFull（付掉勳章後的背包放不下）
    public static HonourOutcome Exchange(
        Character character,
        HonourExchange? exchange,
        IReadOnlyList<InventoryEntry> bag,
        MercenaryContract? current,
        Func<string, ItemDefinition> items,
        Func<string, MercenaryDefinition> mercenaries,
        DateTimeOffset now)
    {
        if (exchange is null)
        {
            return new HonourOutcome(new DeliveryPlan([], [], []), null, null, ["honourUnknown"]);
        }

        var payment = Delivery.Plan(bag, exchange.Cost);
        var reasons = new List<string>();
        if (payment.Missing.Count > 0)
        {
            reasons.Add("medalsMissing");
        }

        if (exchange.MercenaryId is { } mercenaryId)
        {
            if (current is not null)
            {
                reasons.Add("mercenaryHired");
            }

            var contract = MercenaryRules.NewContract(mercenaries(mercenaryId), now);
            return reasons.Count > 0 ? Rejected(payment, reasons) : new HonourOutcome(payment, null, contract, []);
        }

        var item = items(exchange.ItemId!);
        var sameCategory = Delivery.After(bag, payment).Where(entry => items(entry.ItemId).Category == item.Category).ToList();
        var added = InventoryRules.TryAdd(sameCategory, item, exchange.Quantity, character.BagBonus);
        if (added.Discarded > 0)
        {
            reasons.Add("bagFull");
        }

        return reasons.Count > 0 ? Rejected(payment, reasons) : new HonourOutcome(payment, added, null, []);
    }

    private static HonourOutcome Rejected(DeliveryPlan payment, IReadOnlyList<string> reasons) => new(payment, null, null, reasons);
}
