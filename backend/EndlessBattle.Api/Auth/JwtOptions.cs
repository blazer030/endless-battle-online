namespace EndlessBattle.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ebo";

    public string Audience { get; init; } = "ebo";

    // 至少 32 bytes；正式環境用環境變數 Jwt__SigningKey 提供
    public string SigningKey { get; init; } = "";

    public int LifetimeDays { get; init; } = 30;
}
