namespace Inkbound.Domain.Characters;

// 創角輸入違反規則；Application 層轉成 InputValidationException
public sealed class CharacterCreationException(string message) : Exception(message);
