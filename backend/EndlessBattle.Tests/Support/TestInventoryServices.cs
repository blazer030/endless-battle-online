using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;

namespace EndlessBattle.Tests.Support;

// 只用到背包的服務測試：寵物、熟練度、密藥與冒險推進都用空的 in-memory 版本，與背包共用同一個交易
public static class TestInventoryServices
{
    public static InventoryService Create(
        ICharacterRepository characters,
        IInventoryRepository inventory,
        IEquipmentRepository equipment,
        IGameContent content,
        IClock clock,
        IRandomSourceFactory random,
        IUnitOfWork unitOfWork)
    {
        var pets = new InMemoryPetRepository();
        var proficiency = new InMemoryJobProficiencyRepository();
        var buffs = new InMemoryBuffRepository();
        var adventure = new AdventureService(
            characters, proficiency, new InMemorySkillBookRepository(), new InMemorySkillLoadoutRepository(), inventory, equipment,
            new InMemoryMissionRepository(), pets, new InMemoryMercenaryRepository(), new InMemoryAreaPassRepository(), buffs,
            content, clock, random, unitOfWork);
        return new InventoryService(characters, inventory, equipment, pets, proficiency, buffs, content, clock, random, unitOfWork, adventure);
    }
}
