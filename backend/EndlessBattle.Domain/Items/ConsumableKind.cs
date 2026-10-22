namespace EndlessBattle.Domain.Items;

// Capture：捕捉道具，Amount 是能力倍率；StoneBox：Amount 是抽出的魔石顆數；RefineBox：Amount 是精鍊石等級；
// PetEgg：Amount 是能力倍率；ProficiencyBook：Amount 是技能點；ExperienceCapsule：Amount 是經驗值；
// Elixir：效果在 ItemDefinition.Elixir
public enum ConsumableKind
{
    None,
    Supply,
    StatPotion,
    StaminaPotion,
    BagExpansion,
    DepotExpansion,
    Capture,
    StoneBox,
    RefineBox,
    PetEgg,
    ProficiencyBook,
    ExperienceCapsule,
    Elixir,
}
