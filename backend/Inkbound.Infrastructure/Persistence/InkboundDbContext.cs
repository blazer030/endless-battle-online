using System;
using System.Collections.Generic;
using Inkbound.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkbound.Infrastructure.Persistence;

public partial class InkboundDbContext : DbContext
{
    public InkboundDbContext(DbContextOptions<InkboundDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Character> Characters { get; set; }

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
            entity.Property(e => e.CurrentStageId)
                .HasMaxLength(64)
                .HasColumnName("current_stage_id");
            entity.Property(e => e.Element)
                .HasDefaultValueSql("'1'")
                .HasColumnName("element");
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
