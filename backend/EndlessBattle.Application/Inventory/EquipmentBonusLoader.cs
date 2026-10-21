using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Equipment;

namespace EndlessBattle.Application.Inventory;

public static class EquipmentBonusLoader
{
    public static async Task<EquipmentBonus> LoadBonusAsync(
        this IEquipmentRepository equipment, long characterId, IGameContent content, CancellationToken cancellationToken) =>
        EquipmentBonusCalculator.Sum(await equipment.GetAsync(characterId, cancellationToken), content.GetItem);
}
