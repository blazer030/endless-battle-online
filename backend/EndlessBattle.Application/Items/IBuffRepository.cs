using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Items;

// 每個角色每種密藥至多一筆；到期的列留著，由讀取端依時刻判斷
public interface IBuffRepository
{
    // 依種類
    Task<IReadOnlyList<CharacterBuff>> ListAsync(long characterId, CancellationToken cancellationToken);

    // 同種覆蓋
    Task SaveAsync(long characterId, CharacterBuff buff, CancellationToken cancellationToken);
}
