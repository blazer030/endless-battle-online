namespace EndlessBattle.Domain.Forge;

internal static class ForgeGuard
{
    // 呼叫端應先以 Check 回傳原因代碼；走到這裡代表呼叫端漏檢
    public static void Ensure(IReadOnlyList<string> reasons)
    {
        if (reasons.Count > 0)
        {
            throw new InvalidOperationException($"不符合鍛造條件：{string.Join(", ", reasons)}");
        }
    }
}
