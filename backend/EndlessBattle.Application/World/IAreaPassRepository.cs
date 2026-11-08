using EndlessBattle.Domain.World;

namespace EndlessBattle.Application.World;

// 每個角色每個禁區至多一張通行證
public interface IAreaPassRepository
{
    Task<AreaPass?> FindAsync(long characterId, int placeId, CancellationToken cancellationToken);

    // 依地區 id
    Task<IReadOnlyList<AreaPass>> ListAsync(long characterId, CancellationToken cancellationToken);

    // 同地區覆蓋
    Task SaveAsync(long characterId, AreaPass pass, CancellationToken cancellationToken);
}
