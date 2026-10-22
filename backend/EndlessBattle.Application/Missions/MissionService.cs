using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Application.Missions;

// 寫入型用例各是一筆交易：讀角色與紀錄 → 去掉過期與未知 → 驗證 → 以版本鎖寫入角色（內容不變、只遞增版本）
// → 刪除過期每日紀錄 → 寫任務表。看板只讀，不刪過期紀錄
public sealed class MissionService(
    ICharacterRepository characters,
    IMissionRepository missions,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IGameContent content,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    public async Task<MissionBoardView> GetBoardAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        return await BoardAsync(character, clock.UtcNow, cancellationToken);
    }

    public Task<MissionBoardView> AcceptAsync(long accountId, string missionId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var now = clock.UtcNow;
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var records = await missions.ListAsync(character.Id, cancellationToken);
            var current = MissionBoard.Current(records, content.AllMissions, now);
            var mission = content.AllMissions.FirstOrDefault(definition => definition.Id == missionId);
            var reasons = MissionRules.CanAccept(character, mission, current);
            if (reasons.Count > 0)
            {
                throw new InputValidationException("無法接取任務", reasons);
            }

            await LockAndRemoveExpiredAsync(character, records, now, cancellationToken);
            var period = mission!.Daily ? DailyMissions.PeriodOf(now) : (DateOnly?)null;
            await missions.AddAsync(character.Id, new MissionRecord(mission.Id, MissionState.Active, 0, period, now, null), cancellationToken);
            return await BoardAsync(character, now, cancellationToken);
        }, cancellationToken);

    // 放棄＝刪除紀錄，之後可再接
    public Task<MissionBoardView> AbandonAsync(long accountId, string missionId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var now = clock.UtcNow;
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var records = await missions.ListAsync(character.Id, cancellationToken);
            var current = MissionBoard.Current(records, content.AllMissions, now);
            if (!current.Any(record => record.MissionId == missionId && record.State == MissionState.Active))
            {
                throw new InputValidationException("任務不在進行中", ["missionNotActive"]);
            }

            await LockAndRemoveExpiredAsync(character, records, now, cancellationToken);
            await missions.RemoveAsync(character.Id, missionId, cancellationToken);
            return await BoardAsync(character, now, cancellationToken);
        }, cancellationToken);

    // 交易中第一個寫入是角色版本鎖，同一角色的並行請求因版本不符收到 409
    private async Task LockAndRemoveExpiredAsync(
        Character character, IReadOnlyList<MissionRecord> records, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await characters.UpdateAsync(character, cancellationToken);
        foreach (var record in records.Where(record => DailyMissions.IsExpired(record, now)))
        {
            await missions.RemoveAsync(character.Id, record.MissionId, cancellationToken);
        }
    }

    private async Task<MissionBoardView> BoardAsync(Character character, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var records = await missions.ListAsync(character.Id, cancellationToken);
        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        return MissionBoard.Build(character, content.AllMissions, records, Delivery.Owned(state.Bag), now);
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
