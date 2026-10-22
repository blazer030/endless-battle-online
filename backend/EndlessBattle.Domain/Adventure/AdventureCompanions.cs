using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Domain.Adventure;

// 推進時隨行的同伴；CarriedPet 為攜帶中的寵物，null 表示沒有。PetCount 是全部寵物數（攜帶＋獸欄），
// Capture 是補給欄的捕捉道具，null 表示沒有
public sealed record AdventureCompanions(Pet? CarriedPet = null, int PetCount = 0, CaptureState? Capture = null);
