using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Application.World;

public sealed record WorldPlaceView(
    int Id,
    string Name,
    IReadOnlyList<SubPlaceDefinition> SubPlaces,
    LevelRange Recommended,
    bool Restricted,
    bool Current,
    Recommendation Recommendation);

public sealed record WorldAreaView(string Id, string Name, IReadOnlyList<WorldPlaceView> Places);

public sealed class WorldService(
    ICharacterRepository characters,
    IGameContent content,
    IClock clock,
    AdventureService adventure)
{
    public async Task<IReadOnlyList<WorldAreaView>> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        return content.AllAreas.Select(area => new WorldAreaView(
            area.Id,
            area.Name,
            area.PlaceIds.Select(content.GetPlace).Select(place => new WorldPlaceView(
                place.Id,
                place.Name,
                place.SubPlaces,
                place.Recommended,
                place.Restricted,
                place.Id == character.PlaceId,
                PlaceRecommendation.For(place, character.Level))).ToList())).ToList();
    }

    // 先把到現在為止的時間在舊地區結算，再以結算後的版本寫入新位置；推進或寫入撞版本都丟 ConflictException
    public async Task<CharacterProfile> MoveAsync(long accountId, int placeId, int? subPlace, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var target = content.AllPlaces.FirstOrDefault(place => place.Id == placeId);
        // 驗證用的是第一次讀到的角色；期間若有並行變更，由最後寫入時的版本鎖擋下
        var reasons = PlaceMove.Evaluate(character, target, subPlace);
        if (reasons.Count > 0)
        {
            throw new InputValidationException("無法移動", reasons);
        }

        var settled = await adventure.AdvanceAsync(accountId, cancellationToken);
        var updated = await characters.UpdateAsync(settled.Character with { PlaceId = placeId, SubPlace = subPlace }, cancellationToken);
        return new CharacterProfile(CharacterStamina.Regenerated(updated, clock.UtcNow), settled.JobProficiency, settled.Equipment);
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
