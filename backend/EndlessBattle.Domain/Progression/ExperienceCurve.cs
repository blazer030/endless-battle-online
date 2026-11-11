namespace EndlessBattle.Domain.Progression;

// Lv1 的下一級經驗固定 1000，由創角寫定，不經過這條曲線
public static class ExperienceCurve
{
    public static long NextLevelExperienceFor(int level) => 6L * level * level + 1080L * level;
}
