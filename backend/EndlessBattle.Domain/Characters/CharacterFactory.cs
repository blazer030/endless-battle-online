using EndlessBattle.Domain.Content;

namespace EndlessBattle.Domain.Characters;

public static class CharacterFactory
{
    public const int TotalPoints = 10;
    public const int StartingBonus = 10;
    public const int UnallocatedStatValue = 10;
    public const int StartingHp = 50;
    public const int StartingSp = 25;
    public const long StartingMoney = 2000;
    public const int StartingLevel = 1;
    public const long StartingNextExp = 1000;
    public const int StartingJobTier = 1;

    // 違反規則時丟 CharacterCreationException
    public static Character Create(
        long accountId,
        CreateCharacterCommand command,
        JobDefinition job,
        int startingPlaceId,
        int? startingSubPlace,
        DateTimeOffset now)
    {
        var allocation = command.Allocation;
        ValidateAllocation(allocation);
        ValidateChoice(command.Sex, CharacterOptions.Sexes.Count, "性別");
        ValidateChoice(command.Element, CharacterOptions.Elements.Count, "屬性");
        ValidateChoice(command.Birth, CharacterOptions.Births.Count, "出生地");
        if (job.Tier != StartingJobTier)
        {
            throw new CharacterCreationException("只能選擇初階職業");
        }

        return new Character(
            Id: 0,
            AccountId: accountId,
            Name: command.Name,
            Level: StartingLevel,
            Exp: 0,
            NextExp: StartingNextExp,
            Money: StartingMoney,
            Sex: command.Sex,
            Element: command.Element,
            Birth: command.Birth,
            JobId: job.Id,
            Base: new BaseStats(
                Strength: allocation.Strength + StartingBonus,
                Intelligence: allocation.Intelligence + StartingBonus,
                Life: allocation.Life + StartingBonus,
                Vitality: UnallocatedStatValue,
                Agility: allocation.Agility + StartingBonus,
                Charisma: UnallocatedStatValue,
                Faith: UnallocatedStatValue),
            Hp: StartingHp,
            Sp: StartingSp,
            PlaceId: startingPlaceId,
            SubPlace: startingSubPlace,
            LastSettledAt: now,
            Bank: 0,
            BagBonus: 0,
            DepotBonus: 0,
            Version: 0);
    }

    // 每項至少 1 點、總和剛好 10（WOG 允許沒分完，這裡不允許：沒分完沒有好處，多半是誤點）
    private static void ValidateAllocation(StatAllocation allocation)
    {
        int[] points = [allocation.Strength, allocation.Intelligence, allocation.Life, allocation.Agility];
        if (points.Any(point => point < 1))
        {
            throw new CharacterCreationException("每項能力至少要分配 1 點");
        }

        // 先擋單項過大，避免加總溢位
        if (points.Any(point => point > TotalPoints) || points.Sum() != TotalPoints)
        {
            throw new CharacterCreationException($"能力點數必須剛好分完 {TotalPoints} 點");
        }
    }

    private static void ValidateChoice(int value, int count, string label)
    {
        if (value < 1 || value > count)
        {
            throw new CharacterCreationException($"{label}選項無效");
        }
    }
}
