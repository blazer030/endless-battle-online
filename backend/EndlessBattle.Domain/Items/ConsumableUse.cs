using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Items;

// 使用道具時需要的狀態：目前職業（升級成長）、寵物數（只有寵物蛋會看）、全部道具（寶箱的候選）、寵物蛋的物種候選
public sealed record UseContext(
    Character Character, JobDefinition Job, int PetCount, IReadOnlyList<ItemDefinition> AllItems, IReadOnlyList<MonsterDefinition> EggMonsters);

// GrantedItemIds 依抽出順序、各 1 個；Pet 放獸欄；SkillPoints 加在目前職業；Buff 以同種覆蓋寫入
public sealed record UseOutcome(
    Character Character, IReadOnlyList<string> GrantedItemIds, CapturedPet? Pet, long SkillPoints, int LevelsGained, CharacterBuff? Buff = null);

public static class ConsumableUse
{
    // 魔石寶箱 rand(1,100)：≤ 60 等級 1、61～90 等級 2、91～100 等級 3
    private const int ClassOneMax = 60, ClassTwoMax = 90;

    // 原因代碼：notUsable（補給品、裝備、材料等不能從背包直接使用）、noEffect（擴充格數不比目前多）、petsFull（寵物蛋）
    public static IReadOnlyList<string> CanUse(UseContext context, ItemDefinition item) => item.Consumable switch
    {
        ConsumableKind.StatPotion => [],
        ConsumableKind.BagExpansion => item.Amount > context.Character.BagBonus ? [] : ["noEffect"],
        ConsumableKind.DepotExpansion => item.Amount > context.Character.DepotBonus ? [] : ["noEffect"],
        ConsumableKind.StoneBox or ConsumableKind.RefineBox or ConsumableKind.ProficiencyBook or ConsumableKind.ExperienceCapsule
            or ConsumableKind.Elixir => [],
        ConsumableKind.PetEgg => context.PetCount >= PetRules.MaxPets ? ["petsFull"] : [],
        _ => ["notUsable"],
    };

    // 呼叫前先以 CanUse 確認可用
    public static UseOutcome Apply(UseContext context, ItemDefinition item, DateTimeOffset now, IRandomSource random)
    {
        var character = context.Character;
        return item.Consumable switch
        {
            ConsumableKind.StatPotion => Changed(character with { Base = item.Bonus.AddTo(character.Base) }),
            // 擴充是設定成道具的格數，不累加
            ConsumableKind.BagExpansion => Changed(character with { BagBonus = item.Amount }),
            ConsumableKind.DepotExpansion => Changed(character with { DepotBonus = item.Amount }),
            ConsumableKind.StoneBox => Granted(character, OpenStoneBox(context.AllItems, item.Amount, random)),
            ConsumableKind.RefineBox => Granted(character, [OpenRefineBox(context.AllItems, item.Amount, random)]),
            ConsumableKind.PetEgg => new UseOutcome(character, [], Hatch(context.EggMonsters, item.Amount, now, random), 0, 0),
            // 熟練度不變，只加技能點
            ConsumableKind.ProficiencyBook => new UseOutcome(character, [], null, item.Amount, 0),
            ConsumableKind.ExperienceCapsule => GainExperience(context, item.Amount, random),
            ConsumableKind.Elixir when item.Elixir is { } elixir => new UseOutcome(character, [], null, 0, 0, Buffs.Start(elixir, now)),
            _ => throw new InvalidOperationException($"道具 {item.Id} 不能使用"),
        };
    }

    private static UseOutcome Changed(Character character) => new(character, [], null, 0, 0);

    private static UseOutcome Granted(Character character, IReadOnlyList<string> itemIds) => new(character, itemIds, null, 0, 0);

    // 先擲等級，候選是 BoxClass 在 1～等級的魔石（依內容順序），逐顆 NextInt(剩餘數) 抽出並移除，不重複
    private static List<string> OpenStoneBox(IReadOnlyList<ItemDefinition> items, int count, IRandomSource random)
    {
        var roll = PhpRandom.Between(random, 1, 100);
        var boxClass = roll <= ClassOneMax ? 1 : roll <= ClassTwoMax ? 2 : 3;
        var candidates = items.Where(item => item.Stone is { BoxClass: >= 1 } stone && stone.BoxClass <= boxClass).ToList();
        var drawn = new List<string>();
        while (drawn.Count < count && candidates.Count > 0)
        {
            var index = random.NextInt(candidates.Count);
            drawn.Add(candidates[index].Id);
            candidates.RemoveAt(index);
        }

        return drawn;
    }

    private static string OpenRefineBox(IReadOnlyList<ItemDefinition> items, int level, IRandomSource random)
    {
        var candidates = items.Where(item => item.Refine?.Level == level).ToList();
        return candidates[random.NextInt(candidates.Count)].Id;
    }

    // 物種從非任務怪中 NextInt 抽，再以 PetCapture.NewPet 擲能力與個性
    private static CapturedPet Hatch(IReadOnlyList<MonsterDefinition> monsters, int multiplier, DateTimeOffset now, IRandomSource random)
    {
        var candidates = monsters.Where(monster => monster.MissionId is null).ToList();
        return PetCapture.NewPet(candidates[random.NextInt(candidates.Count)], multiplier, now, random);
    }

    // 加經驗後立刻升級
    private static UseOutcome GainExperience(UseContext context, int experience, IRandomSource random)
    {
        var levelUp = LevelUp.Apply(context.Character with { Experience = context.Character.Experience + experience }, context.Job, random, atSeconds: 0);
        return new UseOutcome(levelUp.Character, [], null, 0, levelUp.Events.Count);
    }
}
