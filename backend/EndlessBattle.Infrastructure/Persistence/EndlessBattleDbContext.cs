using System;
using System.Collections.Generic;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public partial class EndlessBattleDbContext : DbContext
{
    public EndlessBattleDbContext(DbContextOptions<EndlessBattleDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Character> Characters { get; set; }

    public virtual DbSet<CharacterAreaPass> CharacterAreaPasses { get; set; }

    public virtual DbSet<CharacterBuff> CharacterBuffs { get; set; }

    public virtual DbSet<CharacterChampion> CharacterChampions { get; set; }

    public virtual DbSet<CharacterDepot> CharacterDepots { get; set; }

    public virtual DbSet<CharacterEquipment> CharacterEquipments { get; set; }

    public virtual DbSet<CharacterInventory> CharacterInventories { get; set; }

    public virtual DbSet<CharacterJobProficiency> CharacterJobProficiencies { get; set; }

    public virtual DbSet<CharacterMercenary> CharacterMercenaries { get; set; }

    public virtual DbSet<CharacterMission> CharacterMissions { get; set; }

    public virtual DbSet<CharacterPet> CharacterPets { get; set; }

    public virtual DbSet<CharacterSkill> CharacterSkills { get; set; }

    public virtual DbSet<CharacterSkillSlot> CharacterSkillSlots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("accounts");

            entity.HasIndex(e => e.Username, "uk_accounts_username").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasColumnName("created_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Username)
                .HasMaxLength(32)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("characters");

            entity.HasIndex(e => e.AccountId, "uk_characters_account_id").IsUnique();

            entity.HasIndex(e => e.Name, "uk_characters_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.ActNum)
                .HasDefaultValueSql("'50'")
                .HasColumnName("act_num");
            entity.Property(e => e.ActNumTime)
                .HasMaxLength(6)
                .HasColumnName("act_num_time");
            entity.Property(e => e.AttackPreference).HasColumnName("attack_preference");
            entity.Property(e => e.BagBonus).HasColumnName("bag_bonus");
            entity.Property(e => e.Bank).HasColumnName("bank");
            entity.Property(e => e.BaseAgi)
                .HasDefaultValueSql("'10'")
                .HasColumnName("base_agi");
            entity.Property(e => e.BaseAu)
                .HasDefaultValueSql("'10'")
                .HasColumnName("base_au");
            entity.Property(e => e.BaseBe)
                .HasDefaultValueSql("'10'")
                .HasColumnName("base_be");
            entity.Property(e => e.BaseLife)
                .HasDefaultValueSql("'10'")
                .HasColumnName("base_life");
            entity.Property(e => e.BaseSmart)
                .HasDefaultValueSql("'10'")
                .HasColumnName("base_smart");
            entity.Property(e => e.BaseStr)
                .HasDefaultValueSql("'10'")
                .HasColumnName("base_str");
            entity.Property(e => e.BaseVit)
                .HasDefaultValueSql("'10'")
                .HasColumnName("base_vit");
            entity.Property(e => e.Birth)
                .HasDefaultValueSql("'1'")
                .HasColumnName("birth");
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentPlaceId)
                .HasDefaultValueSql("'1'")
                .HasColumnName("current_place_id");
            entity.Property(e => e.CurrentStageId)
                .HasMaxLength(64)
                .HasColumnName("current_stage_id");
            entity.Property(e => e.CurrentSubPlace).HasColumnName("current_sub_place");
            entity.Property(e => e.DepotBonus).HasColumnName("depot_bonus");
            entity.Property(e => e.Element)
                .HasDefaultValueSql("'1'")
                .HasColumnName("element");
            entity.Property(e => e.EscapeHpPercent).HasColumnName("escape_hp_percent");
            entity.Property(e => e.Exp).HasColumnName("exp");
            entity.Property(e => e.Hp)
                .HasDefaultValueSql("'50'")
                .HasColumnName("hp");
            entity.Property(e => e.JobId)
                .HasMaxLength(32)
                .HasDefaultValueSql("'warrior'")
                .HasColumnName("job_id");
            entity.Property(e => e.LastSettledAt)
                .HasMaxLength(6)
                .HasColumnName("last_settled_at");
            entity.Property(e => e.Level)
                .HasDefaultValueSql("'1'")
                .HasColumnName("level");
            entity.Property(e => e.Money).HasColumnName("money");
            entity.Property(e => e.Name)
                .HasMaxLength(12)
                .HasColumnName("name");
            entity.Property(e => e.NextExp)
                .HasDefaultValueSql("'1000'")
                .HasColumnName("next_exp");
            entity.Property(e => e.PotionHpPercent)
                .HasDefaultValueSql("'15'")
                .HasColumnName("potion_hp_percent");
            entity.Property(e => e.PotionSpPercent)
                .HasDefaultValueSql("'100'")
                .HasColumnName("potion_sp_percent");
            entity.Property(e => e.RestHpPercent)
                .HasDefaultValueSql("'15'")
                .HasColumnName("rest_hp_percent");
            entity.Property(e => e.RestSpPercent)
                .HasDefaultValueSql("'15'")
                .HasColumnName("rest_sp_percent");
            entity.Property(e => e.Sex)
                .HasDefaultValueSql("'1'")
                .HasColumnName("sex");
            entity.Property(e => e.Sp)
                .HasDefaultValueSql("'25'")
                .HasColumnName("sp");
            entity.Property(e => e.SpecialAttackName)
                .HasMaxLength(16)
                .HasDefaultValueSql("'必殺'")
                .HasColumnName("special_attack_name");
            entity.Property(e => e.Version).HasColumnName("version");

            entity.HasOne(d => d.Account).WithOne(p => p.Character)
                .HasForeignKey<Character>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_characters_account");
        });

        modelBuilder.Entity<CharacterAreaPass>(entity =>
        {
            entity.HasKey(e => new { e.CharacterId, e.PlaceId }).HasName("PRIMARY");

            entity.ToTable("character_area_passes");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.PlaceId).HasColumnName("place_id");
            entity.Property(e => e.ExpiresAt)
                .HasMaxLength(6)
                .HasColumnName("expires_at");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterAreaPasses)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_area_passes_character");
        });

        modelBuilder.Entity<CharacterBuff>(entity =>
        {
            entity.HasKey(e => new { e.CharacterId, e.Kind }).HasName("PRIMARY");

            entity.ToTable("character_buffs");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Kind)
                .HasMaxLength(16)
                .HasColumnName("kind");
            entity.Property(e => e.EndsAt)
                .HasMaxLength(6)
                .HasColumnName("ends_at");
            entity.Property(e => e.Multiplier)
                .HasPrecision(4)
                .HasColumnName("multiplier");
            entity.Property(e => e.StartsAt)
                .HasMaxLength(6)
                .HasColumnName("starts_at");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterBuffs)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_buffs_character");
        });

        modelBuilder.Entity<CharacterChampion>(entity =>
        {
            entity.HasKey(e => e.CharacterId).HasName("PRIMARY");

            entity.ToTable("character_champions");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Agi).HasColumnName("agi");
            entity.Property(e => e.At).HasColumnName("at");
            entity.Property(e => e.Au).HasColumnName("au");
            entity.Property(e => e.CrownedAt)
                .HasMaxLength(6)
                .HasColumnName("crowned_at");
            entity.Property(e => e.Defenses).HasColumnName("defenses");
            entity.Property(e => e.Df).HasColumnName("df");
            entity.Property(e => e.Element).HasColumnName("element");
            entity.Property(e => e.Hp).HasColumnName("hp");
            entity.Property(e => e.HpMax).HasColumnName("hp_max");
            entity.Property(e => e.IsPlayer).HasColumnName("is_player");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.LockedUntil)
                .HasMaxLength(6)
                .HasColumnName("locked_until");
            entity.Property(e => e.Mat).HasColumnName("mat");
            entity.Property(e => e.Mdf).HasColumnName("mdf");
            entity.Property(e => e.Name)
                .HasMaxLength(12)
                .HasColumnName("name");
            entity.Property(e => e.Sp).HasColumnName("sp");
            entity.Property(e => e.SpMax).HasColumnName("sp_max");
            entity.Property(e => e.SpecialAttackName)
                .HasMaxLength(16)
                .HasColumnName("special_attack_name");

            entity.HasOne(d => d.Character).WithOne(p => p.CharacterChampion)
                .HasForeignKey<CharacterChampion>(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_champions_character");
        });

        modelBuilder.Entity<CharacterDepot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("character_depot");

            entity.HasIndex(e => e.CharacterId, "ix_character_depot_character");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Holes).HasColumnName("holes");
            entity.Property(e => e.ItemId)
                .HasMaxLength(32)
                .HasColumnName("item_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.RefineBonus)
                .HasColumnType("json")
                .HasColumnName("refine_bonus");
            entity.Property(e => e.RefineLevel).HasColumnName("refine_level");
            entity.Property(e => e.Sockets)
                .HasColumnType("json")
                .HasColumnName("sockets");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterDepots)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_depot_character");
        });

        modelBuilder.Entity<CharacterEquipment>(entity =>
        {
            entity.HasKey(e => new { e.CharacterId, e.Slot }).HasName("PRIMARY");

            entity.ToTable("character_equipment");

            entity.HasIndex(e => e.InventoryId, "fk_character_equipment_inventory");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Slot)
                .HasMaxLength(16)
                .HasColumnName("slot");
            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.SupplyQuantity).HasColumnName("supply_quantity");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterEquipments)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_equipment_character");

            entity.HasOne(d => d.Inventory).WithMany(p => p.CharacterEquipments)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_equipment_inventory");
        });

        modelBuilder.Entity<CharacterInventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("character_inventory");

            entity.HasIndex(e => e.CharacterId, "ix_character_inventory_character");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Holes).HasColumnName("holes");
            entity.Property(e => e.ItemId)
                .HasMaxLength(32)
                .HasColumnName("item_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'1'")
                .HasColumnName("quantity");
            entity.Property(e => e.RefineBonus)
                .HasColumnType("json")
                .HasColumnName("refine_bonus");
            entity.Property(e => e.RefineLevel).HasColumnName("refine_level");
            entity.Property(e => e.Sockets)
                .HasColumnType("json")
                .HasColumnName("sockets");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterInventories)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_inventory_character");
        });

        modelBuilder.Entity<CharacterJobProficiency>(entity =>
        {
            entity.HasKey(e => new { e.CharacterId, e.JobId }).HasName("PRIMARY");

            entity.ToTable("character_job_proficiency");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.JobId)
                .HasMaxLength(32)
                .HasColumnName("job_id");
            entity.Property(e => e.Proficiency).HasColumnName("proficiency");
            entity.Property(e => e.SkillPoints).HasColumnName("skill_points");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterJobProficiencies)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_job_proficiency_character");
        });

        modelBuilder.Entity<CharacterMercenary>(entity =>
        {
            entity.HasKey(e => e.CharacterId).HasName("PRIMARY");

            entity.ToTable("character_mercenaries");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.HiredAt)
                .HasMaxLength(6)
                .HasColumnName("hired_at");
            entity.Property(e => e.MercenaryId)
                .HasMaxLength(32)
                .HasColumnName("mercenary_id");
            entity.Property(e => e.Name)
                .HasMaxLength(8)
                .HasColumnName("name");
            entity.Property(e => e.NextFightAt)
                .HasMaxLength(6)
                .HasColumnName("next_fight_at");
            entity.Property(e => e.PlaceId).HasColumnName("place_id");
            entity.Property(e => e.Remaining).HasColumnName("remaining");

            entity.HasOne(d => d.Character).WithOne(p => p.CharacterMercenary)
                .HasForeignKey<CharacterMercenary>(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_mercenaries_character");
        });

        modelBuilder.Entity<CharacterMission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("character_missions");

            entity.HasIndex(e => new { e.CharacterId, e.MissionId }, "ux_character_missions_character_mission").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AcceptedAt)
                .HasMaxLength(6)
                .HasColumnName("accepted_at");
            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.CompletedAt)
                .HasMaxLength(6)
                .HasColumnName("completed_at");
            entity.Property(e => e.DailyPeriod)
                .HasColumnType("date")
                .HasColumnName("daily_period");
            entity.Property(e => e.MissionId)
                .HasMaxLength(32)
                .HasColumnName("mission_id");
            entity.Property(e => e.Progress).HasColumnName("progress");
            entity.Property(e => e.State)
                .HasMaxLength(16)
                .HasColumnName("state");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterMissions)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_missions_character");
        });

        modelBuilder.Entity<CharacterPet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("character_pets");

            entity.HasIndex(e => e.CharacterId, "ix_character_pets_character");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Attack).HasColumnName("attack");
            entity.Property(e => e.BornAt)
                .HasMaxLength(6)
                .HasColumnName("born_at");
            entity.Property(e => e.Carried).HasColumnName("carried");
            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Defense).HasColumnName("defense");
            entity.Property(e => e.HungerSince)
                .HasMaxLength(6)
                .HasColumnName("hunger_since");
            entity.Property(e => e.Intimacy).HasColumnName("intimacy");
            entity.Property(e => e.Magic).HasColumnName("magic");
            entity.Property(e => e.MonsterId)
                .HasMaxLength(32)
                .HasColumnName("monster_id");
            entity.Property(e => e.Name)
                .HasMaxLength(12)
                .HasColumnName("name");
            entity.Property(e => e.Personality).HasColumnName("personality");
            entity.Property(e => e.Satiety).HasColumnName("satiety");
            entity.Property(e => e.Spirit).HasColumnName("spirit");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterPets)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_pets_character");
        });

        modelBuilder.Entity<CharacterSkill>(entity =>
        {
            entity.HasKey(e => new { e.CharacterId, e.SkillId }).HasName("PRIMARY");

            entity.ToTable("character_skills");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.SkillId)
                .HasMaxLength(32)
                .HasColumnName("skill_id");
            entity.Property(e => e.Level).HasColumnName("level");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterSkills)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_skills_character");
        });

        modelBuilder.Entity<CharacterSkillSlot>(entity =>
        {
            entity.HasKey(e => new { e.CharacterId, e.Slot }).HasName("PRIMARY");

            entity.ToTable("character_skill_slots");

            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.Slot).HasColumnName("slot");
            entity.Property(e => e.SkillId)
                .HasMaxLength(32)
                .HasColumnName("skill_id");

            entity.HasOne(d => d.Character).WithMany(p => p.CharacterSkillSlots)
                .HasForeignKey(d => d.CharacterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_character_skill_slots_character");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
