using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Characters;
using Microsoft.EntityFrameworkCore;
using CharacterRow = EndlessBattle.Infrastructure.Persistence.Models.Character;

namespace EndlessBattle.Infrastructure.Persistence;

// 在 scaffold 產生的資料列（CharacterRow）與 Domain 的 Character 之間轉換
public sealed class CharacterRepository(EndlessBattleDbContext context) : ICharacterRepository
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
            Version = character.Version,
            CreatedAt = character.LastSettledAt.UtcDateTime,
        };
        CopyState(character, row);
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

    public async Task<Character> UpdateAsync(Character character, CancellationToken cancellationToken)
    {
        var row = await context.Characters.SingleOrDefaultAsync(existing => existing.Id == character.Id, cancellationToken)
            ?? throw new NotFoundException("找不到角色");

        // 以呼叫端拿到的版本作為比對基準：UPDATE ... WHERE version = character.Version
        context.Entry(row).Property(existing => existing.Version).OriginalValue = character.Version;
        CopyState(character, row);
        row.Version = character.Version + 1;
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("角色資料已被其他請求更新，請重新整理");
        }

        return ToDomain(row);
    }

    // 新增與更新共用；scaffold 的欄位帶有資料庫預設值，值為 0 時 EF 會在 INSERT 略過該欄位改用預設值
    private static void CopyState(Character character, CharacterRow row)
    {
        row.Level = character.Level;
        row.Exp = character.Exp;
        row.NextExp = character.NextExp;
        row.Money = character.Money;
        row.Sex = (sbyte)character.Sex;
        row.Element = (sbyte)character.Element;
        row.Birth = (sbyte)character.Birth;
        row.JobId = character.JobId;
        row.SpecialAttackName = character.SpecialAttackName;
        row.BaseStr = character.Base.Str;
        row.BaseSmart = character.Base.Smart;
        row.BaseLife = character.Base.Life;
        row.BaseVit = character.Base.Vit;
        row.BaseAgi = character.Base.Agi;
        row.BaseAu = character.Base.Au;
        row.BaseBe = character.Base.Be;
        row.Hp = character.Hp;
        row.Sp = character.Sp;
        row.ActNum = character.ActNum;
        row.ActNumTime = character.ActNumTime?.UtcDateTime;
        row.CurrentStageId = character.CurrentStageId;
        row.LastSettledAt = character.LastSettledAt.UtcDateTime;
    }

    private static Character ToDomain(CharacterRow row) => new(
        Id: row.Id,
        AccountId: row.AccountId,
        Name: row.Name,
        Level: row.Level,
        Exp: row.Exp,
        NextExp: row.NextExp,
        Money: row.Money,
        Sex: row.Sex,
        Element: row.Element,
        Birth: row.Birth,
        JobId: row.JobId,
        SpecialAttackName: row.SpecialAttackName,
        Base: new BaseStats(row.BaseStr, row.BaseSmart, row.BaseLife, row.BaseVit, row.BaseAgi, row.BaseAu, row.BaseBe),
        Hp: row.Hp,
        Sp: row.Sp,
        ActNum: row.ActNum,
        ActNumTime: row.ActNumTime is { } actNumTime ? ToUtc(actNumTime) : null,
        CurrentStageId: row.CurrentStageId,
        LastSettledAt: ToUtc(row.LastSettledAt),
        Version: row.Version);

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
