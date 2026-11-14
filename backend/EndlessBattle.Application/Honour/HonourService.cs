using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Application.Mercenaries;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Honour;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Missions;

namespace EndlessBattle.Application.Honour;

// Met：角色裸點是否達到（同裝備時的判斷）
public sealed record HonourRequirement(BaseStat Stat, int Minimum, bool Met);

// Requirements：道具兌換的裝備需求，只列大於 0 的
public sealed record HonourOffer(HonourExchange Exchange, bool Affordable, IReadOnlyList<HonourRequirement> Requirements);

// Medals：每種勳章（內容順序）的背包持有數；HiredMercenaryName：目前契約的名字
public sealed record HonourOverview(IReadOnlyList<HonourOffer> Offers, IReadOnlyDictionary<string, int> Medals, string? HiredMercenaryName);

// 兌換一筆交易：讀取與驗證 → 角色版本鎖 → 扣勳章 → 加道具或新增傭兵合約
public sealed class HonourService(
    ICharacterRepository characters,
    IInventoryRepository inventory,
    IEquipmentRepository equipment,
    IMercenaryRepository mercenaries,
    IGameContent content,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    public async Task<HonourOverview> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        return await OverviewAsync(character, cancellationToken);
    }

    public Task<HonourOverview> ExchangeAsync(long accountId, string exchangeId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
            var outcome = HonourShop.Exchange(
                character,
                content.HonourShop.FirstOrDefault(exchange => exchange.Id == exchangeId),
                state.Bag,
                await mercenaries.FindAsync(character.Id, cancellationToken),
                content.GetItem,
                content.GetMercenary,
                clock.UtcNow);
            if (outcome.Reasons.Count > 0)
            {
                throw new InputValidationException("無法兌換", outcome.Reasons);
            }

            await characters.UpdateAsync(character, cancellationToken);
            foreach (var entry in outcome.Payment.Updated)
            {
                await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity, cancellationToken);
            }

            foreach (var id in outcome.Payment.Removed)
            {
                await inventory.RemoveAsync(character.Id, id, cancellationToken);
            }

            if (outcome.Item is { } item)
            {
                foreach (var entry in item.UpdatedEntries)
                {
                    await inventory.UpdateQuantityAsync(character.Id, entry.Id, entry.Quantity, cancellationToken);
                }

                foreach (var entry in item.NewEntries)
                {
                    await inventory.AddAsync(character.Id, entry, cancellationToken);
                }
            }

            if (outcome.Mercenary is { } contract)
            {
                await mercenaries.AddAsync(character.Id, contract, cancellationToken);
            }

            return await OverviewAsync(character, cancellationToken);
        }, cancellationToken);

    private async Task<HonourOverview> OverviewAsync(Character character, CancellationToken cancellationToken)
    {
        var state = await BagReader.LoadAsync(inventory, equipment, character.Id, cancellationToken);
        var owned = Delivery.Owned(state.Bag);
        var offers = content.HonourShop
            .Select(exchange => new HonourOffer(exchange, HonourShop.Affordable(exchange, owned), Requirements(character, exchange)))
            .ToList();
        var medals = content.AllItems
            .Where(item => item.Category == ItemCategory.Medal)
            .ToDictionary(item => item.Id, item => owned.GetValueOrDefault(item.Id));
        var contract = await mercenaries.FindAsync(character.Id, cancellationToken);
        return new HonourOverview(offers, medals, contract?.Name);
    }

    private static readonly BaseStat[] RequirementStats = [BaseStat.Strength, BaseStat.Intelligence, BaseStat.Agility, BaseStat.Charisma];

    private IReadOnlyList<HonourRequirement> Requirements(Character character, HonourExchange exchange)
    {
        if (exchange.ItemId is not { } itemId)
        {
            return [];
        }

        var item = content.GetItem(itemId);
        var reasons = EquipmentRules.CanEquip(character, item);
        return RequirementStats
            .Select(stat => (stat, minimum: Minimum(item.Requires, stat)))
            .Where(requirement => requirement.minimum > 0)
            .Select(requirement => new HonourRequirement(
                requirement.stat, requirement.minimum, !reasons.Contains(BaseStatCode.StatTooLow(requirement.stat))))
            .ToList();
    }

    private static int Minimum(EquipmentRequirement requires, BaseStat stat) => stat switch
    {
        BaseStat.Strength => requires.Strength,
        BaseStat.Intelligence => requires.Intelligence,
        BaseStat.Agility => requires.Agility,
        BaseStat.Charisma => requires.Charisma,
        _ => 0,
    };

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
