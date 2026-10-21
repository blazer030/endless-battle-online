using EndlessBattle.Domain.Randomness;

namespace EndlessBattle.Domain.Battle;

public static class PhpRandom
{
    // 與 PHP rand(a, b) 相同：兩端都含，a > b 時對調
    public static int Between(IRandomSource random, int low, int high)
    {
        if (low > high)
        {
            (low, high) = (high, low);
        }

        return random.NextInt(high - low + 1) + low;
    }

    // PHP 先把浮點參數截成整數（往 0 截），再比大小對調
    public static int Between(IRandomSource random, double low, double high) =>
        Between(random, (int)low, (int)high);
}
