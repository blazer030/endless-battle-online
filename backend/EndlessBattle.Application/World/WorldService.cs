using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Application.World;

// Keys 是背包裡這個地區的鑰匙總數；PassExpiresAt 是通行證到期時刻（可能已過期），兩者只有禁區才有
public sealed record WorldPlaceView(
    int Id,
    string Name,
    IReadOnlyList<SubPlaceDefinition> SubPlaces,
    LevelRange Recommended,
    bool Restricted,
    bool Current,
    Recommendation Recommendation,
    int Keys,
    DateTimeOffset? PassExpiresAt);

public sealed record WorldAreaView(string Id, string Name, IReadOnlyList<WorldPlaceView> Places);

public sealed class WorldService(
    ICharacterRepository characters,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IAreaPassRepository areaPasses,
    IGameContent content,
    IClock clock,
    IUnitOfWork unitOfWork,
    AdventureService adventure)
{
    public async Task<IReadOnlyList<WorldAreaView>> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var bag = (await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken)).Bag;
        var passes = (await areaPasses.ListAsync(character.Id, cancellationToken)).ToDictionary(pass => pass.PlaceId);
        var keyItems = KeyItems();
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
                PlaceRecommendation.For(place, character.Level),
                bag.Where(entry => keyItems.GetValueOrDefault(entry.ItemId)?.Key?.PlaceId == place.Id).Sum(entry => entry.Quantity),
                passes.GetValueOrDefault(place.Id)?.ExpiresAt)).ToList())).ToList();
    }

    // 先把到現在為止的時間在舊地區結算，再以結算後的版本寫入新位置；推進或寫入撞版本都丟 ConflictException。
    // 進入禁區時沒有有效通行證就扣一把鑰匙、開新的通行證
    public async Task<CharacterProfile> MoveAsync(long accountId, int placeId, int? subPlace, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var target = content.AllPlaces.FirstOrDefault(place => place.Id == placeId);
        var now = clock.UtcNow;
        var keyItems = KeyItems();
        AreaPass? pass = null;
        InventoryEntry? key = null;
        if (target is { Restricted: true })
        {
            pass = await areaPasses.FindAsync(character.Id, target.Id, cancellationToken);
            key = await FindKeyAsync(character.Id, target.Id, keyItems, cancellationToken);
        }

        // 驗證用的是第一次讀到的角色；期間若有並行變更，由最後寫入時的版本鎖擋下
        var reasons = PlaceMove.Evaluate(character, target, subPlace, pass, key is not null, now);
        if (target is null || reasons.Count > 0)
        {
            throw new InputValidationException("無法移動", reasons);
        }

        var settled = await adventure.AdvanceAsync(accountId, cancellationToken);
        return await unitOfWork.RunAsync(async cancellationToken =>
        {
            // 角色的版本更新最先，同一角色的並行請求在這一列上排隊；之後才重讀背包扣鑰匙
            var updated = await characters.UpdateAsync(settled.Character with { PlaceId = placeId, SubPlace = subPlace }, cancellationToken);
            if (AreaKeys.NeedsKey(target, pass, now))
            {
                var held = await FindKeyAsync(updated.Id, placeId, keyItems, cancellationToken)
                    ?? throw new ConflictException("鑰匙已被其他請求使用");
                if (held.Quantity > 1)
                {
                    await inventory.UpdateQuantityAsync(updated.Id, held.Id, held.Quantity - 1, cancellationToken);
                }
                else
                {
                    await inventory.RemoveAsync(updated.Id, held.Id, cancellationToken);
                }

                var keySpec = keyItems[held.ItemId].Key ?? throw new InvalidOperationException("KeyItems 只收有 key 的道具");
                await areaPasses.SaveAsync(updated.Id, AreaKeys.Enter(keySpec, now), cancellationToken);
            }

            return new CharacterProfile(CharacterStamina.Regenerated(updated, now), settled.JobProficiency, settled.Equipment);
        }, cancellationToken);
    }

    private Dictionary<string, ItemDefinition> KeyItems() =>
        content.AllItems.Where(item => item.Key is not null).ToDictionary(item => item.Id);

    private async Task<InventoryEntry?> FindKeyAsync(
        long characterId, int placeId, Dictionary<string, ItemDefinition> keyItems, CancellationToken cancellationToken)
    {
        var bag = (await BagReader.LoadAsync(inventory, equipment, characterId, cancellationToken)).Bag;
        return AreaKeys.FindKey(bag, placeId, keyItems.GetValueOrDefault);
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
