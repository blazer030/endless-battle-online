using EndlessBattle.Domain.Rest;

namespace EndlessBattle.Application.Rest;

// 每次入住一筆；同一時刻至多一筆生效
public interface ILodgingRepository
{
    // now 這一刻生效的那筆（開始 ≤ now < 結束）
    Task<Lodging?> FindActiveAsync(long characterId, DateTimeOffset now, CancellationToken cancellationToken);

    // 結束時刻晚於 after 的所有住宿，依開始時刻排序（冒險結算用）
    Task<IReadOnlyList<Lodging>> ListEndingAfterAsync(long characterId, DateTimeOffset after, CancellationToken cancellationToken);

    // 入住：ended（依開始時刻對上）改成截斷後的結束時刻、新增 booked、刪掉結束早於 pruneBefore 的
    Task SaveBookingAsync(long characterId, Lodging booked, Lodging? ended, DateTimeOffset pruneBefore, CancellationToken cancellationToken);

    Task<Lodging?> FindAsync(long characterId, CancellationToken cancellationToken);

    // 整筆取代
    Task SaveAsync(long characterId, Lodging lodging, CancellationToken cancellationToken);
}
