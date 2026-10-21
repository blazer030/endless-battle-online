using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：行為與 CharacterRepository 一致，包括版本檢查
public sealed class InMemoryCharacterRepository : ICharacterRepository
{
    private readonly Dictionary<long, Character> charactersByAccount = [];
    private long nextId = 1;

    public Task<Character?> FindByAccountAsync(long accountId, CancellationToken cancellationToken) =>
        Task.FromResult(charactersByAccount.GetValueOrDefault(accountId));

    public Task<Character> AddAsync(Character character, CancellationToken cancellationToken)
    {
        if (charactersByAccount.ContainsKey(character.AccountId))
        {
            throw new ConflictException("此帳號已有角色");
        }

        var stored = character with { Id = nextId++ };
        charactersByAccount[character.AccountId] = stored;
        return Task.FromResult(stored);
    }

    public Task<Character> UpdateAsync(Character character, CancellationToken cancellationToken)
    {
        var existing = charactersByAccount[character.AccountId];
        if (existing.Version != character.Version)
        {
            throw new ConflictException("版本不符");
        }

        var stored = character with { Version = character.Version + 1 };
        charactersByAccount[character.AccountId] = stored;
        return Task.FromResult(stored);
    }
}
