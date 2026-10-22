using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Application.Pets;

// 全部以 characterId 為範圍，時間以 UTC 存取。
// UpdateAsync、RemoveAsync 遇到不存在或屬於別的角色的寵物時不做事也不丟例外，狀態由服務層先驗證
public interface IPetRepository
{
    // Id 升冪
    Task<IReadOnlyList<Pet>> ListAsync(long characterId, CancellationToken cancellationToken);

    // 忽略 pet.Id，回傳新 Id
    Task<long> AddAsync(long characterId, Pet pet, CancellationToken cancellationToken);

    Task UpdateAsync(long characterId, Pet pet, CancellationToken cancellationToken);

    Task RemoveAsync(long characterId, long petId, CancellationToken cancellationToken);
}
