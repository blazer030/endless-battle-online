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
            entity.Property(e => e.CreatedAt)
                .HasMaxLength(6)
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentStageId)
                .HasMaxLength(64)
                .HasColumnName("current_stage_id");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.LastSettledAt)
                .HasMaxLength(6)
                .HasColumnName("last_settled_at");
            entity.Property(e => e.Name)
                .HasMaxLength(12)
                .HasColumnName("name");
            entity.Property(e => e.SpiritStones).HasColumnName("spirit_stones");
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
