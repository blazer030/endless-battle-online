using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Pets;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Application.Missions;

// 寫入型用例各是一筆交易：讀角色與紀錄 → 去掉過期與未知 → 驗證 → 以版本鎖寫入角色（接取與放棄內容不變、只遞增版本；
// 完成時一併寫入獎勵）→ 刪除過期與種類不符的紀錄 → 寫其他表。看板只讀，不刪這些紀錄
public sealed class MissionService(
    ICharacterRepository characters,
    IMissionRepository missions,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    ISkillBookRepository skillBook,
    IPetRepository pets,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory,
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
            if (mission is null || reasons.Count > 0)
            {
                throw new InputValidationException("無法接取任務", reasons);
            }

            await LockAndRemoveStaleAsync(character, records, now, cancellationToken);
            var period = mission.Daily ? DailyMissions.PeriodOf(now) : (DateOnly?)null;
            await missions.AddAsync(character.Id, new MissionRecord(mission.Id, MissionState.Active, [], period, now, null), cancellationToken);
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

            await LockAndRemoveStaleAsync(character, records, now, cancellationToken);
            await missions.RemoveAsync(character.Id, missionId, cancellationToken);
            return await BoardAsync(character, now, cancellationToken);
        }, cancellationToken);

    // 驗證依序 missionNotActive、objectiveIncomplete、itemsMissing、petMissing、獎勵（bagFull、petsFull），全部通過才寫入；
    // 寫入順序：角色（版本鎖＋獎勵）→ 過期與種類不符的紀錄 → 交付 → 獎勵道具 → 技能 → 交出的寵物 → 獎勵寵物 → 任務
    public Task<MissionCompletion> CompleteAsync(long accountId, string missionId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var now = clock.UtcNow;
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var records = await missions.ListAsync(character.Id, cancellationToken);
            var record = MissionBoard.Current(records, content.AllMissions, now)
                .FirstOrDefault(current => current.MissionId == missionId && current.State == MissionState.Active)
                ?? throw new InputValidationException("無法完成任務", ["missionNotActive"]);
            var mission = content.GetMission(missionId);
            var objective = DailyMissions.Resolve(mission, character.Id, record.DailyPeriod);
            if (!MissionBoard.Ready(objective, record.ProgressOf(0)))
            {
                throw new InputValidationException("無法完成任務", ["objectiveIncomplete"]);
            }

            var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
            var delivery = Delivery.Plan(state.Bag, objective.Deliver);
            if (delivery.Missing.Count > 0)
            {
                throw new InputValidationException("無法完成任務", ["itemsMissing"]);
            }

            // 交寵物要交攜帶中的那隻（PHP pe_st=0 and pe_m_id）
            var owned = await pets.ListAsync(character.Id, cancellationToken);
            Pet? deliveredPet = null;
            if (objective.DeliverPet is { } species)
            {
                deliveredPet = owned.FirstOrDefault(pet => pet.Carried && pet.MonsterId == species)
                    ?? throw new InputValidationException("無法完成任務", ["petMissing"]);
            }

            var learned = await skillBook.ListAsync(character.Id, cancellationToken);
            var outcome = MissionRewards.Apply(
                character, content.GetJob(character.JobId), objective, Delivery.After(state.Bag, delivery), content.GetItem, content.GetMonster, learned,
                owned.Count - (deliveredPet is null ? 0 : 1), now, randomFactory.Create());
            if (outcome.Reasons.Count > 0)
            {
                throw new InputValidationException("無法完成任務", outcome.Reasons);
            }

            var updated = await LockAndRemoveStaleAsync(outcome.Character, records, now, cancellationToken);
            foreach (var entry in delivery.Updated)
            {
                await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity, cancellationToken);
            }

            foreach (var id in delivery.Removed)
            {
                await inventory.RemoveAsync(character.Id, id, cancellationToken);
            }

            foreach (var entry in outcome.UpdatedEntries)
            {
                await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity, cancellationToken);
            }

            foreach (var entry in outcome.NewEntries)
            {
                await inventory.AddAsync(character.Id, entry, cancellationToken);
            }

            if (outcome.GrantedSkill is { } skill)
            {
                await skillBook.UpsertAsync(character.Id, skill, cancellationToken);
            }

            if (deliveredPet is not null)
            {
                await pets.RemoveAsync(character.Id, deliveredPet.Id, cancellationToken);
            }

            if (outcome.NewPet is { } newPet)
            {
                await pets.AddAsync(character.Id, newPet.ToPet(), cancellationToken);
            }

            await missions.CompleteAsync(character.Id, missionId, now, cancellationToken);
            var reward = objective.Reward;
            return new MissionCompletion(
                await BoardAsync(updated, now, cancellationToken),
                mission.CompletionMessage,
                objective.Exp,
                reward.Money,
                reward.Items,
                reward.Skill,
                outcome.GrantedSkill is not null,
                outcome.LevelsGained,
                reward.Pet);
        }, cancellationToken);

    // 交易中第一個寫入是角色版本鎖，同一角色的並行請求因版本不符收到 409；回傳寫入後的角色
    private async Task<Character> LockAndRemoveStaleAsync(
        Character character, IReadOnlyList<MissionRecord> records, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var updated = await characters.UpdateAsync(character, cancellationToken);
        foreach (var record in MissionBoard.Stale(records, content.AllMissions, now))
        {
            await missions.RemoveAsync(character.Id, record.MissionId, cancellationToken);
        }

        return updated;
    }

    private async Task<MissionBoardView> BoardAsync(Character character, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var records = await missions.ListAsync(character.Id, cancellationToken);
        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var carried = (await pets.ListAsync(character.Id, cancellationToken)).FirstOrDefault(pet => pet.Carried);
        return MissionBoard.Build(character, content.AllMissions, records, Delivery.Owned(state.Bag), now, carried?.MonsterId);
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
