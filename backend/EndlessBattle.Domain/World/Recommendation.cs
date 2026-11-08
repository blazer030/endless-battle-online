namespace EndlessBattle.Domain.World;

// None：禁區，不給提示
public enum Recommendation
{
    Recommended,
    TooHard,
    TooEasy,
    None,
}

public static class PlaceRecommendation
{
    // 等級等於上下限也算推薦
    public static Recommendation For(PlaceDefinition place, int level) =>
        place.Restricted ? Recommendation.None
        : level < place.Recommended.Min ? Recommendation.TooHard
        : level > place.Recommended.Max ? Recommendation.TooEasy
        : Recommendation.Recommended;
}
