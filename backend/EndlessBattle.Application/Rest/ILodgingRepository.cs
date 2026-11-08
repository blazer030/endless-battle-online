using EndlessBattle.Domain.Rest;

namespace EndlessBattle.Application.Rest;

// 每個角色至多一筆目前的入住；到期的列留著，由讀取端依時刻判斷
public interface ILodgingRepository
{
    Task<Lodging?> FindAsync(long characterId, CancellationToken cancellationToken);

    // 整筆取代
    Task SaveAsync(long characterId, Lodging lodging, CancellationToken cancellationToken);
}
