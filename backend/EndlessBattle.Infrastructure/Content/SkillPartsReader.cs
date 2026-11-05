using System.Text.Json;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Infrastructure.Content;

// 讀技能新格式每一級的 conditions、costs、effects；種類與欄位不認得、缺必填欄位都直接拒絕，訊息帶技能 id 與等級
internal sealed class SkillPartsReader(string skillId, int level)
{
    private string Where => $"技能 {skillId} 第 {level} 級";

    public SkillCondition Condition(JsonElement element)
    {
        var kind = Enum<SkillConditionKind>(element, "kind", "條件");
        Allow(element, kind.ToString(), "kind", "percent");
        return new SkillCondition(kind, Int(element, "percent"));
    }

    public SkillCost Cost(JsonElement element)
    {
        var kind = Enum<SkillCostKind>(element, "kind", "消耗");
        Allow(element, kind.ToString(), "kind", "amount");
        return new SkillCost(kind, Int(element, "amount"));
    }

    public SkillEffect Effect(JsonElement element)
    {
        var kind = String(element, "kind", "效果");
        var rounds = OptionalInt(element, "rounds") ?? 0;
        switch (kind)
        {
            case "StatChange":
                Allow(element, kind, "kind", "rounds", "target", "stat", "percent", "amount", "scaling", "chance");
                var percent = OptionalDouble(element, "percent");
                var amount = OptionalInt(element, "amount");
                var scaling = OptionalScaling(element);
                if (new object?[] { percent, amount, scaling }.Count(value => value is not null) != 1)
                {
                    throw Rejected($"的效果 StatChange 必須恰好有 percent、amount、scaling 其中一項");
                }

                return new StatChangeEffect(
                    Enum<SkillTarget>(element, "target", "對象"), Enum<SkillStat>(element, "stat", "能力"), percent, amount, scaling, rounds)
                {
                    Chance = OptionalInt(element, "chance"),
                };
            case "BonusDamage":
                Allow(element, kind, "kind", "rounds", "attackKind", "scaling", "chance");
                return new BonusDamageEffect(
                    Enum<SkillAttackKind>(element, "attackKind", "攻擊方式"),
                    OptionalScaling(element) ?? throw Rejected("的效果 BonusDamage 必須有 scaling"),
                    rounds)
                {
                    Chance = OptionalInt(element, "chance"),
                };
            case "MultiHit":
                Allow(element, kind, "kind", "rounds", "attackKind", "hits");
                return new MultiHitEffect(Enum<SkillAttackKind>(element, "attackKind", "攻擊方式"), Int(element, "hits"), rounds);
            case "DamageReduction":
                Allow(element, kind, "kind", "rounds", "attackKind", "percent");
                return new DamageReductionEffect(Enum<SkillAttackKind>(element, "attackKind", "攻擊方式"), Double(element, "percent"), rounds);
            case "Restore":
                Allow(element, kind, "kind", "rounds", "resource", "scaling", "percentOfDamage", "percentOfMaximum");
                var restoreScaling = OptionalScaling(element);
                var percentOfDamage = OptionalDouble(element, "percentOfDamage");
                var percentOfMaximum = OptionalDouble(element, "percentOfMaximum");
                if (new object?[] { restoreScaling, percentOfDamage, percentOfMaximum }.Count(value => value is not null) != 1)
                {
                    throw Rejected("的效果 Restore 必須恰好有 scaling、percentOfDamage、percentOfMaximum 其中一項");
                }

                return new RestoreEffect(Enum<SkillResource>(element, "resource", "資源"), restoreScaling, percentOfDamage, rounds)
                {
                    PercentOfMaximum = percentOfMaximum,
                };
            case "Stun":
                Allow(element, kind, "kind", "rounds", "chance");
                return new StunEffect(Int(element, "chance"), rounds);
            case "Silence":
                Allow(element, kind, "kind", "rounds", "chance");
                return new SilenceEffect(Int(element, "chance"), rounds);
            case "PercentDamage":
                Allow(element, kind, "kind", "rounds", "percent");
                return new PercentDamageEffect(Double(element, "percent"), rounds);
            case "DamageOverTime":
                Allow(element, kind, "kind", "rounds", "scaling");
                return new DamageOverTimeEffect(OptionalScaling(element) ?? throw Rejected("的效果 DamageOverTime 必須有 scaling"), rounds);
            case "ReduceSkillPoints":
                Allow(element, kind, "kind", "rounds", "amount", "percentOfMaximum");
                var lossAmount = OptionalInt(element, "amount");
                var lossPercent = OptionalDouble(element, "percentOfMaximum");
                if ((lossAmount is null) == (lossPercent is null))
                {
                    throw Rejected("的效果 ReduceSkillPoints 必須恰好有 amount、percentOfMaximum 其中一項");
                }

                return new ReduceSkillPointsEffect(lossAmount, lossPercent, rounds);
            case "AttackElement":
                Allow(element, kind, "kind", "rounds", "element");
                return new AttackElementEffect(Element(element), rounds);
            case "DefenseElement":
                Allow(element, kind, "kind", "rounds", "element");
                return new DefenseElementEffect(Element(element), rounds);
            case "ElementAdvantage":
                Allow(element, kind, "kind", "rounds", "percent");
                return new ElementAdvantageEffect(Double(element, "percent"), rounds);
            default:
                throw Rejected($"的效果種類無法辨識：{kind}");
        }
    }

    // 內容檔寫屬性名稱，轉成 ElementTable 的 1～6
    private static readonly string[] ElementNames = ["Earth", "Water", "Fire", "Wood", "Wind", "Poison"];

    private int Element(JsonElement element)
    {
        var name = String(element, "element", "屬性");
        var index = Array.FindIndex(ElementNames, candidate => candidate.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index + 1 : throw Rejected($"的屬性無法辨識：{name}");
    }

    private IReadOnlyList<SkillScaling>? OptionalScaling(JsonElement element)
    {
        if (!element.TryGetProperty("scaling", out var scaling))
        {
            return null;
        }

        if (scaling.ValueKind != JsonValueKind.Array || scaling.GetArrayLength() == 0)
        {
            throw Rejected("的 scaling 必須是至少一項的陣列");
        }

        return [.. scaling.EnumerateArray().Select(term =>
        {
            Allow(term, "scaling", "basis", "ratio");
            return new SkillScaling(Enum<SkillScalingBasis>(term, "basis", "能力基準"), Double(term, "ratio"));
        })];
    }

    private void Allow(JsonElement element, string kind, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Contains(property.Name))
            {
                throw Rejected($"的 {kind} 不能有 {property.Name}");
            }
        }
    }

    private T Enum<T>(JsonElement element, string name, string label) where T : struct, Enum
    {
        var value = String(element, name, label);
        return System.Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && System.Enum.IsDefined(parsed)
            ? parsed
            : throw Rejected($"的{label}無法辨識：{value}");
    }

    private string String(JsonElement element, string name, string label) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw Rejected($"的{label}缺少 {name}");

    private int Int(JsonElement element, string name) =>
        OptionalInt(element, name) ?? throw Rejected($"缺少 {name}");

    private double Double(JsonElement element, string name) =>
        OptionalDouble(element, name) ?? throw Rejected($"缺少 {name}");

    private int? OptionalInt(JsonElement element, string name) =>
        !element.TryGetProperty(name, out var value) ? null
        : value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number
        : throw Rejected($"的 {name} 必須是整數");

    private double? OptionalDouble(JsonElement element, string name) =>
        !element.TryGetProperty(name, out var value) ? null
        : value.ValueKind == JsonValueKind.Number ? value.GetDouble()
        : throw Rejected($"的 {name} 必須是數字");

    private GameContentException Rejected(string detail) => new($"{Where}{detail}");
}
