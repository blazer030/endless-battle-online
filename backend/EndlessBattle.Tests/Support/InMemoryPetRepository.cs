using EndlessBattle.Application.Pets;
using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：與 PetRepository 一致，更新或移除不存在（或別的角色）的寵物不做事。
// 寫入記為 pet:add|update|remove:{id}
public sealed class InMemoryPetRepository(List<string>? log = null) : IPetRepository, IRestorableRepository
{
    private readonly Dictionary<long, (long CharacterId, Pet Pet)> rows = [];
    private readonly List<string> writes = log ?? [];
    private long nextId = 1;

    public Task<IReadOnlyList<Pet>> ListAsync(long characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Pet>>(rows.Values
            .Where(row => row.CharacterId == characterId)
            .Select(row => row.Pet)
            .OrderBy(pet => pet.Id)
            .ToList());

    public Task<long> AddAsync(long characterId, Pet pet, CancellationToken cancellationToken)
    {
        var id = nextId++;
        rows[id] = (characterId, pet with { Id = id });
        writes.Add($"pet:add:{id}");
        return Task.FromResult(id);
    }

    public Task UpdateAsync(long characterId, Pet pet, CancellationToken cancellationToken)
    {
        if (rows.TryGetValue(pet.Id, out var row) && row.CharacterId == characterId)
        {
            rows[pet.Id] = (characterId, pet);
            writes.Add($"pet:update:{pet.Id}");
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(long characterId, long petId, CancellationToken cancellationToken)
    {
        if (rows.TryGetValue(petId, out var row) && row.CharacterId == characterId)
        {
            rows.Remove(petId);
            writes.Add($"pet:remove:{petId}");
        }

        return Task.CompletedTask;
    }

    public Action Snapshot()
    {
        var saved = new Dictionary<long, (long CharacterId, Pet Pet)>(rows);
        var savedNextId = nextId;
        return () =>
        {
            rows.Clear();
            foreach (var (id, row) in saved)
            {
                rows[id] = row;
            }

            nextId = savedNextId;
        };
    }
}
