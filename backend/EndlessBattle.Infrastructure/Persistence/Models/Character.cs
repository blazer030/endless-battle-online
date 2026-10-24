using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class Character
{
    public long Id { get; set; }

    public long AccountId { get; set; }

    public string Name { get; set; } = null!;

    public long Exp { get; set; }

    public long Money { get; set; }

    public string? CurrentStageId { get; set; }

    public DateTime LastSettledAt { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public int Level { get; set; }

    public long NextExp { get; set; }

    public sbyte Sex { get; set; }

    public sbyte Element { get; set; }

    public sbyte Birth { get; set; }

    public string JobId { get; set; } = null!;

    public string SpecialAttackName { get; set; } = null!;

    public int BaseStr { get; set; }

    public int BaseSmart { get; set; }

    public int BaseLife { get; set; }

    public int BaseVit { get; set; }

    public int BaseAgi { get; set; }

    public int BaseAu { get; set; }

    public int BaseBe { get; set; }

    public int Hp { get; set; }

    public int Sp { get; set; }

    public int CurrentPlaceId { get; set; }

    public sbyte? CurrentSubPlace { get; set; }

    public long Bank { get; set; }

    public int BagBonus { get; set; }

    public int DepotBonus { get; set; }

    public sbyte? AttackPreference { get; set; }

    public sbyte PotionHpPercent { get; set; }

    public sbyte PotionSpPercent { get; set; }

    public sbyte RestHpPercent { get; set; }

    public sbyte RestSpPercent { get; set; }

    public sbyte EscapeHpPercent { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<CharacterAreaPass> CharacterAreaPasses { get; set; } = new List<CharacterAreaPass>();

    public virtual ICollection<CharacterBuff> CharacterBuffs { get; set; } = new List<CharacterBuff>();

    public virtual CharacterChampion? CharacterChampion { get; set; }

    public virtual ICollection<CharacterDepot> CharacterDepots { get; set; } = new List<CharacterDepot>();

    public virtual ICollection<CharacterEquipment> CharacterEquipments { get; set; } = new List<CharacterEquipment>();

    public virtual ICollection<CharacterInventory> CharacterInventories { get; set; } = new List<CharacterInventory>();

    public virtual ICollection<CharacterJobProficiency> CharacterJobProficiencies { get; set; } = new List<CharacterJobProficiency>();

    public virtual CharacterMercenary? CharacterMercenary { get; set; }

    public virtual ICollection<CharacterMission> CharacterMissions { get; set; } = new List<CharacterMission>();

    public virtual ICollection<CharacterPet> CharacterPets { get; set; } = new List<CharacterPet>();

    public virtual ICollection<CharacterSkillSlot> CharacterSkillSlots { get; set; } = new List<CharacterSkillSlot>();

    public virtual ICollection<CharacterSkill> CharacterSkills { get; set; } = new List<CharacterSkill>();
}
