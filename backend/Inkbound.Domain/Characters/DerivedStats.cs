namespace Inkbound.Domain.Characters;

public sealed record DerivedStats(int At, int Mat, int Df, int Mdf, int HpMax, int SpMax)
{
    // total 是裸點加上裝備加成；本計畫沒有裝備，total 等於裸點
    public static DerivedStats From(BaseStats total) => new(
        At: total.Str,
        Mat: total.Smart,
        Df: total.Vit,
        Mdf: total.Be,
        HpMax: RoundLikePhp(total.Life * 25.0) + 50,
        SpMax: RoundLikePhp((total.Smart + total.Be) * 3.0) + 25);

    // 與 PHP round 相同：.5 遠離零進位
    public static int RoundLikePhp(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
