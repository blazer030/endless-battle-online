using Inkbound.Application.Characters;
using Inkbound.Application.Common;
using Inkbound.Domain.Characters;
using Microsoft.EntityFrameworkCore;
using CharacterRow = Inkbound.Infrastructure.Persistence.Models.Character;

namespace Inkbound.Infrastructure.Persistence;

// 在 scaffold 產生的資料列（CharacterRow）與 Domain 的 Character 之間轉換
public sealed class CharacterRepository(InkboundDbContext context) : ICharacterRepository
{
    public async Task<Character?> FindByAccountAsync(long accountId, CancellationToken cancellationToken)
    {
        var row = await context.Characters.AsNoTracking()
            .SingleOrDefaultAsync(character => character.AccountId == accountId, cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<Character> AddAsync(Character character, CancellationToken cancellationToken)
    {
        var row = new CharacterRow
        {
            AccountId = character.AccountId,
            Name = character.Name,
            Experience = character.Experience,
            SpiritStones = character.SpiritStones,
            CurrentStageId = character.CurrentStageId,
            LastSettledAt = character.LastSettledAt.UtcDateTime,
            Version = character.Version,
            CreatedAt = character.LastSettledAt.UtcDateTime,
        };
        context.Characters.Add(row);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (MySqlErrors.IsDuplicateKey(exception))
        {
            throw new ConflictException("此帳號已有角色，或角色名稱已被使用");
        }

        return ToDomain(row);
    }

    private static Character ToDomain(CharacterRow row) => new(
        row.Id,
        row.AccountId,
        row.Name,
        row.Experience,
        row.SpiritStones,
        row.CurrentStageId,
        new DateTimeOffset(DateTime.SpecifyKind(row.LastSettledAt, DateTimeKind.Utc)),
        row.Version);
}
