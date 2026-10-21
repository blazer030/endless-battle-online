namespace EndlessBattle.Domain.Jobs;

public static class JobProficiencyDisplay
{
    public const long Full = 3500;

    public static int Percent(long proficiency) =>
        (int)Math.Max(0, Math.Min(100, Math.Floor(proficiency / (double)Full * 100)));
}
