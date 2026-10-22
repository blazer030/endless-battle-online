using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Champion;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Application.Champion;

// Reasons：現在挑戰會被擋的原因
public sealed record ChampionView(ChampionSeat Seat, IReadOnlyList<string> Reasons);

public sealed record ChampionChallengeResult(ChampionView View, IReadOnlyList<BattleEvent> Events, bool Won);

// 挑戰一筆交易：讀取與驗證 → 對戰 → 角色版本鎖（扣費、戰後 HP／SP）→ 存席位
public sealed class ChampionService(
    ICharacterRepository characters,
    IChampionRepository champions,
    IEquipmentRepository equipment,
    ISkillLoadoutRepository loadout,
    ISkillBookRepository skillBook,
    IGameContent content,
    IClock clock,
    IRandomSourceFactory randomFactory,
    IUnitOfWork unitOfWork)
{
    public async Task<ChampionView> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var seat = await SeatAsync(character, cancellationToken);
        return new ChampionView(seat, ChampionRules.CanChallenge(character, seat, clock.UtcNow));
    }

    public Task<ChampionChallengeResult> ChallengeAsync(long accountId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var seat = await SeatAsync(character, cancellationToken);
            var now = clock.UtcNow;
            var reasons = ChampionRules.CanChallenge(character, seat, now);
            if (reasons.Count > 0)
            {
                throw new InputValidationException("無法挑戰冠軍", reasons);
            }

            var bonus = await equipment.LoadBonusAsync(character.Id, content, cancellationToken);
            var skills = await EquippedSkills.LoadAsync(loadout, skillBook, content, character, cancellationToken);
            var result = ChampionDuel.Fight(character, bonus, skills, seat, now, randomFactory.Create());

            var updated = await characters.UpdateAsync(result.Character, cancellationToken);
            await champions.SaveAsync(updated.Id, result.Seat, cancellationToken);
            return new ChampionChallengeResult(
                new ChampionView(result.Seat, ChampionRules.CanChallenge(updated, result.Seat, now)), result.Battle.Events, result.Won);
        }, cancellationToken);

    private async Task<ChampionSeat> SeatAsync(Character character, CancellationToken cancellationToken) =>
        await champions.FindAsync(character.Id, cancellationToken) ?? content.SeedChampion;

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
