using EndlessBattle.Application.Common;
using EndlessBattle.Application.Mercenaries;
using EndlessBattle.Domain.Mercenaries;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 MercenaryRepository 一致，已有合約再新增丟 ConflictException，更新或移除不存在的合約不做事。
// 寫入記為 mercenary:add|update|remove
public sealed class InMemoryMercenaryRepository(List<string>? log = null) : IMercenaryRepository, IRestorableRepository
{
    private readonly Dictionary<long, MercenaryContract> rows = [];
    private readonly List<string> writes = log ?? [];

    public Task<MercenaryContract?> FindAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult(rows.GetValueOrDefault(characterId));

    public Task AddAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken)
    {
        if (!rows.TryAdd(characterId, contract))
        {
            throw new ConflictException("已有傭兵合約");
        }

        writes.Add("mercenary:add");
        return Task.CompletedTask;
    }

    public Task UpdateAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken)
    {
        if (rows.ContainsKey(characterId))
        {
            rows[characterId] = contract;
            writes.Add("mercenary:update");
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(long characterId, CancellationToken cancellationToken)
    {
        if (rows.Remove(characterId))
        {
            writes.Add("mercenary:remove");
        }

        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<long, MercenaryContract>(rows);
        return () =>
        {
            rows.Clear();
            foreach (var (characterId, contract) in saved)
            {
                rows[characterId] = contract;
            }
        };
    }
}
