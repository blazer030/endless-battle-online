namespace EndlessBattle.Domain.Characters;

// 創角輸入違反規則；Reason 是原因代碼，Application 層轉成 InputValidationException 時沿用
public sealed class CharacterCreationException(string message, string reason) : Exception(message)
{
    public string Reason { get; } = reason;
}
