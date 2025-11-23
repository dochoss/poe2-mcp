using Microsoft.EntityFrameworkCore;
using Poe2Mcp.Core.Data.Models;

namespace Poe2Mcp.Core.Data;

/// <summary>
/// Entity Framework Core database context for PoE2 game data
/// </summary>
public class Poe2DbContext : DbContext
{
    public Poe2DbContext(DbContextOptions<Poe2DbContext> options) 
        : base(options)
    {
    }

    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<UniqueItem> UniqueItems { get; set; } = null!;
    public DbSet<Modifier> Modifiers { get; set; } = null!;
    public DbSet<PassiveNode> PassiveNodes { get; set; } = null!;
    public DbSet<PassiveConnection> PassiveConnections { get; set; } = null!;
    public DbSet<SkillGem> SkillGems { get; set; } = null!;
    public DbSet<SupportGem> SupportGems { get; set; } = null!;
    public DbSet<Ascendancy> Ascendancies { get; set; } = null!;
    public DbSet<SavedBuild> SavedBuilds { get; set; } = null!;
    public DbSet<GameDataVersion> GameDataVersions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Item entity
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BaseType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ItemClass).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ItemClass);
        });

        // Configure UniqueItem entity
        modelBuilder.Entity<UniqueItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BaseType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ItemClass).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ItemClass);
        });

        // Configure Modifier entity
        modelBuilder.Entity<Modifier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ModType).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name);
        });

        // Configure PassiveNode entity
        modelBuilder.Entity<PassiveNode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NodeId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.HasIndex(e => e.NodeId).IsUnique();
        });

        // Configure PassiveConnection entity
        modelBuilder.Entity<PassiveConnection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.FromNode)
                .WithMany()
                .HasForeignKey(e => e.FromNodeId)
                .HasPrincipalKey(n => n.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.ToNode)
                .WithMany()
                .HasForeignKey(e => e.ToNodeId)
                .HasPrincipalKey(n => n.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SkillGem entity
        modelBuilder.Entity<SkillGem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name);
        });

        // Configure SupportGem entity
        modelBuilder.Entity<SupportGem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name);
        });

        // Configure Ascendancy entity
        modelBuilder.Entity<Ascendancy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BaseClass).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure SavedBuild entity
        modelBuilder.Entity<SavedBuild>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BuildName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.UserId);
        });

        // Configure GameDataVersion entity
        modelBuilder.Entity<GameDataVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataSource).IsRequired().HasMaxLength(100);
        });
    }
}
