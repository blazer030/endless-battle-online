using Inkbound.Domain.Characters;

namespace Inkbound.Application.Characters;

public interface ICharacterRepository
{
    Task<Character?> FindByAccountAsync(long accountId, CancellationToken cancellationToken);

    // 帳號已有角色或名稱重複時丟 ConflictException
    Task<Character> AddAsync(Character character, CancellationToken cancellationToken);

    // 以 character.Version 作為預期版本更新，成功後回傳 Version + 1 的角色；版本不符丟 ConflictException
    Task<Character> UpdateAsync(Character character, CancellationToken cancellationToken);
}
