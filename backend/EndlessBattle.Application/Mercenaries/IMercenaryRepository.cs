using EndlessBattle.Domain.Mercenaries;

namespace EndlessBattle.Application.Mercenaries;

// 每個角色至多一份合約，時間以 UTC 存取。
// UpdateAsync、RemoveAsync 遇到沒有合約時不做事也不丟例外，狀態由服務層先驗證
public interface IMercenaryRepository
{
    Task<MercenaryContract?> FindAsync(long characterId, CancellationToken cancellationToken);

    // 已有一份合約 → ConflictException
    Task AddAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken);

    Task UpdateAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken);

    Task RemoveAsync(long characterId, CancellationToken cancellationToken);
}
