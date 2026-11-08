using EndlessBattle.Domain.Pets;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Pets;

public class PetRulesTests
{
    [Fact]
    public void Carry_StoresThePreviouslyCarried()
    {
        IReadOnlyList<Pet> pets =
        [
            TestPets.Create(name: "甲", carried: true) with { Id = 1 },
            TestPets.Create(name: "乙", carried: false) with { Id = 2 },
            TestPets.Create(name: "丙", carried: false) with { Id = 3 },
        ];

        var carried = PetRules.Carry(pets, petId: 3);

        Assert.Equal([false, false, true], carried.Select(pet => pet.Carried));
        Assert.Equal([1L, 2L, 3L], carried.Select(pet => pet.Id));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("一二三四五六七八九十一二三", false)]
    [InlineData("一二三四五六七八九十一二", true)]
    [InlineData("  小雪  ", true)]
    public void Rename_RejectsEmptyOrTooLong(string? name, bool valid)
    {
        Assert.Equal(valid ? [] : ["nameInvalid"], PetRules.CanRename(name));
    }
}
