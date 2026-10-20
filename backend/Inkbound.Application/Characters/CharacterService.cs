using System.Globalization;
using Inkbound.Application.Common;
using Inkbound.Application.Content;
using Inkbound.Domain.Characters;

namespace Inkbound.Application.Characters;

public sealed class CharacterService(ICharacterRepository characters, IGameContent content, IClock clock)
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 12;

    public async Task<Character> CreateAsync(long accountId, string name, CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();

        // 以「文字單位」計算長度，組合字元與 emoji 都算一個字
        var nameLength = new StringInfo(trimmedName).LengthInTextElements;
        if (nameLength < MinimumNameLength || nameLength > MaximumNameLength)
        {
            throw new InputValidationException($"角色名稱必須是 {MinimumNameLength}～{MaximumNameLength} 個字");
        }

        var character = new Character(
            Id: 0,
            AccountId: accountId,
            Name: trimmedName,
            Experience: 0,
            SpiritStones: 0,
            CurrentStageId: content.StartingStageId,
            LastSettledAt: clock.UtcNow,
            Version: 0);
        return await characters.AddAsync(character, cancellationToken);
    }

    public async Task<Character> GetAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken)
        ?? throw new NotFoundException("尚未建立角色");
}
