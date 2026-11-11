using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Missions;

// Reasons 非空時其餘欄位不可用；NewEntries 的 Id 為 0（尚未寫入）；NewPet 是寵物獎勵擲出的新寵物
public sealed record MissionRewardOutcome(
    Character Character,
    int LevelsGained,
    IReadOnlyList<InventoryEntry> NewEntries,
    IReadOnlyList<InventoryEntry> UpdatedEntries,
    LearnedSkill? GrantedSkill,
    IReadOnlyList<string> Reasons,
    CapturedPet? NewPet = null);

public static class MissionRewards
{
    // 經驗加完立刻升級；道具依序放進交付後的背包，任何一件放不下就整筆拒絕；已會的技能略過、其餘照給；
    // 寵物獎勵在升級擲骰之後擲（PetCapture.NewPet），交付後寵物數已滿就整筆拒絕。Reasons 依序 bagFull、petsFull
    public static MissionRewardOutcome Apply(
        Character character,
        JobDefinition job,
        MissionObjective objective,
        IReadOnlyList<InventoryEntry> bagAfterDelivery,
        Func<string, ItemDefinition> items,
        Func<string, MonsterDefinition> monsters,
        IReadOnlyList<LearnedSkill> learned,
        int petCountAfterDelivery,
        DateTimeOffset now,
        IRandomSource random)
    {
        var reward = objective.Reward;
        var placed = DropApplier.Apply(bagAfterDelivery, reward.Items, items, character.BagBonus);
        var reasons = new List<string>();
        if (placed.Discarded.Contains(true))
        {
            reasons.Add("bagFull");
        }

        if (reward.Pet is not null && petCountAfterDelivery >= PetRules.MaxPets)
        {
            reasons.Add("petsFull");
        }

        if (reasons.Count > 0)
        {
            return new MissionRewardOutcome(character, 0, [], [], null, reasons);
        }

        var rewarded = character with { Experience = character.Experience + objective.Experience, Money = character.Money + reward.Money };
        // 獎勵不是戰鬥中取得，升級事件的時間點沒有意義
        var levelUp = LevelUp.Apply(rewarded, job, random, atSeconds: 0);
        var newPet = reward.Pet is { } pet ? PetCapture.NewPet(monsters(pet.MonsterId), pet.Level, now, random) : null;
        return new MissionRewardOutcome(
            levelUp.Character,
            levelUp.Events.Count,
            placed.NewEntries,
            placed.UpdatedEntries,
            reward.Skill is { } skill && !learned.Any(known => known.SkillId == skill.SkillId)
                ? new LearnedSkill(skill.SkillId, skill.Level)
                : null,
            [],
            newPet);
    }
}
