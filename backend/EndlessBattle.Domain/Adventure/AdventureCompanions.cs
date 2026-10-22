using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Domain.Adventure;

// 推進時隨行的同伴；CarriedPet 為攜帶中的寵物，null 表示沒有
public sealed record AdventureCompanions(Pet? CarriedPet = null);
