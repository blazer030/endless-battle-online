namespace EndlessBattle.Domain.Randomness;

public interface IRandomSource
{
    double NextDouble();

    int NextInt(int maxExclusive);
}
