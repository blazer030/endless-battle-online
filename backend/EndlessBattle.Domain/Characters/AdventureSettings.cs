using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Characters;

// WOG 戰鬥設定面板（js/wog_fight.js）：Attack null 表示自動；補給與住宿 -1 表示略過該條件；逃跑 0 表示不逃
public sealed record AdventureSettings(
    AttackKind? Attack, int PotionHealthPointsPercent, int PotionManaPointsPercent, int RestHealthPointsPercent, int RestManaPointsPercent, int EscapeHealthPointsPercent)
{
    public const int Skip = -1;
    public const int MaximumPercent = 100;

    // 等同計畫 9 的固定行為
    public static AdventureSettings Default { get; } = new(null, 15, 100, 15, 15, 0);

    public IReadOnlyList<string> Validate()
    {
        var reasons = new List<string>();
        if (Attack is { } attack && !Enum.IsDefined(attack))
        {
            reasons.Add("invalidAttack");
        }

        int[] skippable = [PotionHealthPointsPercent, PotionManaPointsPercent, RestHealthPointsPercent, RestManaPointsPercent];
        if (skippable.Any(percent => percent is < Skip or > MaximumPercent) || EscapeHealthPointsPercent is < 0 or > MaximumPercent)
        {
            reasons.Add("invalidPercent");
        }

        return reasons;
    }
}
