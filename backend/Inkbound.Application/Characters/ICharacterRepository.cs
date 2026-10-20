using Inkbound.Domain.Characters;

namespace Inkbound.Application.Characters;

public interface ICharacterRepository
{
    Task<Character?> FindByAccountAsync(long accountId, CancellationToken cancellationToken);

    // 帳號已有角色或名稱重複時丟 ConflictException
    Task<Character> AddAsync(Character character, CancellationToken cancellationToken);
}
