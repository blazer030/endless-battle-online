using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Domain.Missions;

// Reasons 非空（["bagFull"]）時其餘欄位不可用；NewEntries 的 Id 為 0（尚未寫入）
public sealed record MissionRewardOutcome(
    Character Character,
    int LevelsGained,
    IReadOnlyList<InventoryEntry> NewEntries,
    IReadOnlyList<InventoryEntry> UpdatedEntries,
    LearnedSkill? GrantedSkill,
    IReadOnlyList<string> Reasons);

public static class MissionRewards
{
    // 經驗加完立刻升級；道具依序放進交付後的背包，任何一件放不下就整筆拒絕；已會的技能略過、其餘照給
    public static MissionRewardOutcome Apply(
        Character character,
        JobDefinition job,
        MissionObjective objective,
        IReadOnlyList<InventoryEntry> bagAfterDelivery,
        Func<string, ItemDefinition> items,
        IReadOnlyList<LearnedSkill> learned,
        IRandomSource random)
    {
        // 本次新開的格子先給暫時的負數 Id，後面的道具才能疊上去；輸出時再還原成 0
        var working = bagAfterDelivery.ToList();
        var updatedIds = new HashSet<long>();
        var nextTemporaryId = -1L;
        foreach (var reward in objective.Reward.Items)
        {
            var item = items(reward.ItemId);
            var sameCategory = working.Where(entry => items(entry.ItemId).Category == item.Category).ToList();
            var added = InventoryRules.TryAdd(sameCategory, item, reward.Quantity, character.BagBonus);
            if (added.Discarded > 0)
            {
                return new MissionRewardOutcome(character, 0, [], [], null, ["bagFull"]);
            }

            foreach (var entry in added.UpdatedEntries)
            {
                working[working.FindIndex(existing => existing.Id == entry.Id)] = entry;
                updatedIds.Add(entry.Id);
            }

            working.AddRange(added.NewEntries.Select(entry => entry with { Id = nextTemporaryId-- }));
        }

        var rewarded = character with { Exp = character.Exp + objective.Exp, Money = character.Money + objective.Reward.Money };
        // 獎勵不是戰鬥中取得，升級事件的時間點沒有意義
        var levelUp = LevelUp.Apply(rewarded, job, random, atSeconds: 0);
        return new MissionRewardOutcome(
            levelUp.Character,
            levelUp.Events.Count,
            working.Where(entry => entry.Id < 0).Select(entry => entry with { Id = 0 }).ToList(),
            working.Where(entry => entry.Id > 0 && updatedIds.Contains(entry.Id)).ToList(),
            objective.Reward.Skill is { } skill && !learned.Any(known => known.SkillId == skill.SkillId)
                ? new LearnedSkill(skill.SkillId, skill.Level)
                : null,
            []);
    }
}
