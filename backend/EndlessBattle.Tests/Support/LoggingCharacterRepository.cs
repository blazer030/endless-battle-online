using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Tests.Support;

// 記錄更新順序；FailNextUpdate 模擬別的請求先寫入造成的版本衝突
public sealed class LoggingCharacterRepository(List<string> log) : ICharacterRepository
{
    private readonly InMemoryCharacterRepository inner = new();

    public bool FailNextUpdate { get; set; }

    public Task<Character?> FindByAccountAsync(long accountId, CancellationToken cancellationToken) =>
        inner.FindByAccountAsync(accountId, cancellationToken);

    public Task<Character> AddAsync(Character character, CancellationToken cancellationToken) =>
        inner.AddAsync(character, cancellationToken);

    public Task<Character> UpdateAsync(Character character, CancellationToken cancellationToken)
    {
        if (FailNextUpdate)
        {
            FailNextUpdate = false;
            throw new ConflictException("版本不符");
        }

        log.Add("update");
        return inner.UpdateAsync(character, cancellationToken);
    }
}
