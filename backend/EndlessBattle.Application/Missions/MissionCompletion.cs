using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Application.Missions;

// Exp、Money、Items、Skill、Pet 是任務的報酬；SkillGranted 為 false 表示沒有技能獎勵或已會該技能
public sealed record MissionCompletion(
    MissionBoardView Board,
    string CompletionMessage,
    long Exp,
    long Money,
    IReadOnlyList<ItemQuantity> Items,
    SkillReward? Skill,
    bool SkillGranted,
    int LevelsGained,
    PetReward? Pet = null);
