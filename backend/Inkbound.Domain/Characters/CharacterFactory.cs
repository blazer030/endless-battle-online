using System.Globalization;
using Inkbound.Domain.Content;

namespace Inkbound.Domain.Characters;

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
    public const int MaximumSpecialAttackNameLength = 8;

    // 資料庫欄位 VARCHAR(16) 以 code point 計算
    private const int MaximumSpecialAttackNameCodePoints = 16;

    private const int SexCount = 2;
    private const int ElementCount = 6;
    private const int BirthCount = 4;

    // 違反規則時丟 CharacterCreationException
    public static Character Create(
        long accountId,
        CreateCharacterCommand command,
        JobDefinition job,
        string startingStageId,
        DateTimeOffset now)
    {
        var allocation = command.Allocation;
        ValidateAllocation(allocation);
        ValidateChoice(command.Sex, SexCount, "性別");
        ValidateChoice(command.Element, ElementCount, "屬性");
        ValidateChoice(command.Birth, BirthCount, "出生地");
        if (job.Tier != StartingJobTier)
        {
            throw new CharacterCreationException("只能選擇初階職業");
        }

        var specialAttackName = ValidateSpecialAttackName(command.SpecialAttackName);

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
            SpecialAttackName: specialAttackName,
            Base: new BaseStats(
                Str: allocation.Str + StartingBonus,
                Smart: allocation.Smart + StartingBonus,
                Life: allocation.Life + StartingBonus,
                Vit: UnallocatedStatValue,
                Agi: allocation.Agi + StartingBonus,
                Au: UnallocatedStatValue,
                Be: UnallocatedStatValue),
            Hp: StartingHp,
            Sp: StartingSp,
            CurrentStageId: startingStageId,
            LastSettledAt: now,
            Version: 0);
    }

    // 每項至少 1 點、總和不超過 10；沒分完的點數作廢（WOG 如此）
    private static void ValidateAllocation(StatAllocation allocation)
    {
        int[] points = [allocation.Str, allocation.Smart, allocation.Life, allocation.Agi];
        if (points.Any(point => point < 1))
        {
            throw new CharacterCreationException("每項能力至少要分配 1 點");
        }

        if (points.Sum() > TotalPoints)
        {
            throw new CharacterCreationException($"能力點數總和不得超過 {TotalPoints}");
        }
    }

    private static void ValidateChoice(int value, int count, string label)
    {
        if (value < 1 || value > count)
        {
            throw new CharacterCreationException($"{label}選項無效");
        }
    }

    private static string ValidateSpecialAttackName(string name)
    {
        var trimmed = name.Trim();

        // 以「文字單位」計算長度，組合字元與 emoji 都算一個字
        var length = new StringInfo(trimmed).LengthInTextElements;
        if (length < 1 || length > MaximumSpecialAttackNameLength)
        {
            throw new CharacterCreationException($"必殺技名稱必須是 1～{MaximumSpecialAttackNameLength} 個字");
        }

        if (trimmed.EnumerateRunes().Count() > MaximumSpecialAttackNameCodePoints)
        {
            throw new CharacterCreationException("必殺技名稱含有過多組合字元，請改用較簡單的字");
        }

        return trimmed;
    }
}
