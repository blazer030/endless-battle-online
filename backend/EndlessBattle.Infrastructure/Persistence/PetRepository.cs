using EndlessBattle.Application.Pets;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class PetRepository(EndlessBattleDbContext context) : IPetRepository
{
    public async Task<IReadOnlyList<Pet>> ListAsync(long characterId, CancellationToken cancellationToken)
    {
        var rows = await context.CharacterPets.AsNoTracking()
            .Where(row => row.CharacterId == characterId)
            .OrderBy(row => row.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(row => new Pet(
            row.Id, row.MonsterId, row.Name, (PetPersonality)row.Personality, row.Attack, row.Magic, row.Defense,
            row.Spirit, row.Satiety, row.Intimacy, row.Carried, ToUtc(row.BornAt), ToUtc(row.HungerSince))).ToList();
    }

    public async Task<long> AddAsync(long characterId, Pet pet, CancellationToken cancellationToken)
    {
        var row = new CharacterPet { CharacterId = characterId };
        Apply(row, pet);
        context.CharacterPets.Add(row);
        await context.SaveChangesAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateAsync(long characterId, Pet pet, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, pet.Id, cancellationToken);
        if (row is null)
        {
            return;
        }

        Apply(row, pet);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(long characterId, long petId, CancellationToken cancellationToken)
    {
        var row = await FindAsync(characterId, petId, cancellationToken);
        if (row is null)
        {
            return;
        }

        context.CharacterPets.Remove(row);
        await context.SaveChangesAsync(cancellationToken);
    }

    private Task<CharacterPet?> FindAsync(long characterId, long petId, CancellationToken cancellationToken) =>
        context.CharacterPets.SingleOrDefaultAsync(row => row.CharacterId == characterId && row.Id == petId, cancellationToken);

    private static void Apply(CharacterPet row, Pet pet)
    {
        row.MonsterId = pet.MonsterId;
        row.Name = pet.Name;
        row.Personality = (sbyte)pet.Personality;
        row.Attack = pet.Attack;
        row.Magic = pet.Magic;
        row.Defense = pet.Defense;
        row.Spirit = pet.Spirit;
        row.Satiety = pet.Satiety;
        row.Intimacy = pet.Intimacy;
        row.Carried = pet.Carried;
        row.BornAt = pet.BornAt.UtcDateTime;
        row.HungerSince = pet.HungerSince.UtcDateTime;
    }

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
