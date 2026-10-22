using EndlessBattle.Domain.Champion;

namespace EndlessBattle.Application.Champion;

// 以 characterId 為範圍，時間以 UTC 存取
public interface IChampionRepository
{
    // null＝這個角色還沒有存檔，用內容的種子冠軍
    Task<ChampionSeat?> FindAsync(long characterId, CancellationToken cancellationToken);

    // 新增或覆寫；兩個請求同時新增第一筆時，後到的丟 ConflictException
    Task SaveAsync(long characterId, ChampionSeat seat, CancellationToken cancellationToken);
}
