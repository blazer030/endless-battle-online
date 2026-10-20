using Inkbound.Application.Characters;
using Inkbound.Application.Common;
using Inkbound.Application.Content;
using Inkbound.Domain.Adventure;
using Inkbound.Domain.Characters;

namespace Inkbound.Application.Adventure;

public sealed class AdventureService(
    ICharacterRepository characters,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory)
{
    // 一次最多模擬 5 分鐘；更長的離線時間由計畫 3 的離線結算處理
    public const double MaxSimulatedSeconds = 300;

    public async Task<AdvanceResult> AdvanceAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await characters.FindByAccountAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("尚未建立角色");

        var now = clock.UtcNow;
        var elapsedSeconds = (now - character.LastSettledAt).TotalSeconds;

        // 時鐘倒退或剛結算過：什麼都不做，也不寫入
        if (elapsedSeconds <= 0)
        {
            return new AdvanceResult([], 0, 0, character);
        }

        var isCapped = elapsedSeconds > MaxSimulatedSeconds;
        var stage = content.GetStage(character.CurrentStageId);
        var stageMonsters = stage.MonsterIds.Select(content.GetMonster).ToList();
        var run = AdventureSimulator.Run(
            PlayerStats.Starting,
            stageMonsters,
            Math.Min(elapsedSeconds, MaxSimulatedSeconds),
            randomFactory.Create());

        // 沒用完的秒數留到下一次；超過上限時直接結算到現在
        var settledAt = isCapped ? now : character.LastSettledAt.AddSeconds(run.ConsumedSeconds);
        var updated = await characters.UpdateAsync(
            character with
            {
                Experience = character.Experience + run.ExperienceGained,
                SpiritStones = character.SpiritStones + run.SpiritStonesGained,
                LastSettledAt = settledAt,
            },
            cancellationToken);

        return new AdvanceResult(run.Events, run.ExperienceGained, run.SpiritStonesGained, updated);
    }
}
