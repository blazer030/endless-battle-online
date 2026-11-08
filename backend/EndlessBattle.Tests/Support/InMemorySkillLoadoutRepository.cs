using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 SkillLoadoutRepository 一樣先清空再寫入，依欄位排序
public sealed class InMemorySkillLoadoutRepository(List<string>? log = null) : ISkillLoadoutRepository
{
    private readonly Dictionary<long, IReadOnlyList<SkillSlot>> rows = [];

    public List<string> Log { get; } = log ?? [];

    public Task<IReadOnlyList<SkillSlot>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult(rows.GetValueOrDefault(characterId) ?? []);

    public Task ReplaceAsync(long characterId, IReadOnlyList<SkillSlot> slots, CancellationToken cancellationToken)
    {
        Log.Add("loadout");
        rows[characterId] = slots.OrderBy(slot => slot.Slot).ToList();
        return Task.CompletedTask;
    }
}
