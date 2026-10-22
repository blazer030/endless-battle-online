namespace EndlessBattle.Domain.Pets;

public static class PetRules
{
    // 攜帶＋獸欄
    public const int MaxPets = 3, NameMaxLength = 12;

    // 同時只攜帶一隻：指定的攜帶，其餘全部放回獸欄
    public static IReadOnlyList<Pet> Carry(IReadOnlyList<Pet> pets, long petId) =>
        pets.Select(pet => pet with { Carried = pet.Id == petId }).ToList();

    // 去頭尾空白後 1～12 字；以字碼計算，與資料庫 VARCHAR(12) 一致
    public static IReadOnlyList<string> CanRename(string? name)
    {
        var length = name?.Trim().EnumerateRunes().Count() ?? 0;
        return length is >= 1 and <= NameMaxLength ? [] : ["nameInvalid"];
    }
}
