using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Data.Models;
using Xunit;
using FluentAssertions;

// Alias to avoid conflict with Core.Models.Modifier
using DbModifier = Poe2Mcp.Core.Data.Models.Modifier;

namespace Poe2Mcp.Tests.Integration;

/// <summary>
/// Integration tests for database operations
/// </summary>
public class DatabaseIntegrationTests : IDisposable
{
    private readonly Poe2DbContext _context;
    private readonly ServiceProvider _serviceProvider;

    public DatabaseIntegrationTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<Poe2DbContext>(options =>
            options.UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}"));
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<Poe2DbContext>();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SavedBuild_CanBeCreatedAndRetrieved()
    {
        // Arrange
        var build = new SavedBuild
        {
            BuildName = "Test Build",
            CharacterData = "{\"class\":\"Warrior\",\"ascendancy\":\"Titan\",\"level\":95}",
            Notes = "Test notes",
            IsPublic = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.SavedBuilds.Add(build);
        await _context.SaveChangesAsync();

        var retrieved = await _context.SavedBuilds
            .FirstOrDefaultAsync(b => b.BuildName == "Test Build");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.BuildName.Should().Be("Test Build");
        retrieved.CharacterData.Should().Contain("Warrior");
        retrieved.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GameDataVersion_TracksVersionInfo()
    {
        // Arrange
        var version = new GameDataVersion
        {
            DataSource = "items",
            Version = "1.0.0",
            LastUpdated = DateTime.UtcNow
        };

        // Act
        _context.GameDataVersions.Add(version);
        await _context.SaveChangesAsync();

        var retrieved = await _context.GameDataVersions
            .FirstOrDefaultAsync(v => v.DataSource == "items");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task Modifier_CanBeStoredAndQueried()
    {
        // Arrange
        var modifiers = new[]
        {
            new DbModifier { Name = "+# to maximum Life", ModType = "life", StatText = "life_flat" },
            new DbModifier { Name = "#% increased maximum Life", ModType = "life", StatText = "life_percent" },
            new DbModifier { Name = "+#% to Fire Resistance", ModType = "resistance", StatText = "fire_res" }
        };

        // Act
        _context.Modifiers.AddRange(modifiers);
        await _context.SaveChangesAsync();

        var lifeModifiers = await _context.Modifiers
            .Where(m => m.ModType == "life")
            .ToListAsync();

        // Assert
        lifeModifiers.Should().HaveCount(2);
        lifeModifiers.Should().AllSatisfy(m => m.ModType.Should().Be("life"));
    }

    [Fact]
    public async Task SkillGem_WithSupportGems_CanBeLinked()
    {
        // Arrange
        var skillGem = new SkillGem
        {
            Name = "Fireball",
            GemType = "active",
            Tags = "[\"spell\",\"fire\",\"projectile\"]",
            DamageEffectiveness = 120
        };

        var supportGem = new SupportGem
        {
            Name = "Greater Multiple Projectiles",
            Tags = "[\"support\",\"projectile\"]",
            ManaMultiplier = 1.3
        };

        // Act
        _context.SkillGems.Add(skillGem);
        _context.SupportGems.Add(supportGem);
        await _context.SaveChangesAsync();

        var fireSkills = await _context.SkillGems
            .Where(s => s.Tags!.Contains("fire"))
            .ToListAsync();

        var projectileSupports = await _context.SupportGems
            .Where(s => s.Tags!.Contains("projectile"))
            .ToListAsync();

        // Assert
        fireSkills.Should().ContainSingle();
        fireSkills.First().Name.Should().Be("Fireball");
        projectileSupports.Should().ContainSingle();
    }

    [Fact]
    public async Task Item_WithModifiers_StoresComplexData()
    {
        // Arrange
        var item = new Item
        {
            Name = "Rare Helmet",
            BaseType = "Royal Burgonet",
            ItemClass = "Helmet",
            RequiredLevel = 65,
            Properties = "{\"armor\":500,\"evasion\":200}",
            ImplicitMods = "[\"8% increased Global Defences\"]",
            ExplicitMods = "[\"+100 to maximum Life\",\"+40% to Fire Resistance\"]",
            Requirements = "{\"str\":148,\"dex\":68}",
            IsCorrupted = false
        };

        // Act
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var helmet = await _context.Items
            .FirstOrDefaultAsync(i => i.ItemClass == "Helmet");

        // Assert
        helmet.Should().NotBeNull();
        helmet!.Properties.Should().Contain("armor");
        helmet.ExplicitMods.Should().Contain("Life");
    }

    [Fact]
    public async Task Ascendancy_CanBeQueriedByBaseClass()
    {
        // Arrange
        var ascendancies = new[]
        {
            new Ascendancy { Name = "Titan", BaseClass = "Warrior", Description = "Tank build" },
            new Ascendancy { Name = "Warbringer", BaseClass = "Warrior", Description = "Offensive warrior" },
            new Ascendancy { Name = "Stormweaver", BaseClass = "Sorceress", Description = "Lightning mage" }
        };

        // Act
        _context.Ascendancies.AddRange(ascendancies);
        await _context.SaveChangesAsync();

        var warriorAscendancies = await _context.Ascendancies
            .Where(a => a.BaseClass == "Warrior")
            .ToListAsync();

        // Assert
        warriorAscendancies.Should().HaveCount(2);
        warriorAscendancies.Should().AllSatisfy(a => a.BaseClass.Should().Be("Warrior"));
    }

    [Fact]
    public async Task PassiveTree_WithConnections_CanBeNavigated()
    {
        // Arrange
        var nodes = new[]
        {
            new PassiveNode { NodeId = 100, Name = "Start Node", IsMastery = false },
            new PassiveNode { NodeId = 101, Name = "Small Node 1", Stats = "[\"10 Strength\"]" },
            new PassiveNode { NodeId = 102, Name = "Notable Node", IsNotable = true, Stats = "[\"15% increased Damage\"]" }
        };

        _context.PassiveNodes.AddRange(nodes);
        await _context.SaveChangesAsync();

        var connections = new[]
        {
            new PassiveConnection { FromNodeId = 100, ToNodeId = 101 },
            new PassiveConnection { FromNodeId = 101, ToNodeId = 102 }
        };

        _context.PassiveConnections.AddRange(connections);
        await _context.SaveChangesAsync();

        // Act
        var connection = await _context.PassiveConnections
            .Include(c => c.FromNode)
            .Include(c => c.ToNode)
            .FirstOrDefaultAsync(c => c.FromNodeId == 100);

        // Assert
        connection.Should().NotBeNull();
        connection!.FromNode.Should().NotBeNull();
        connection.FromNode!.Name.Should().Be("Start Node");
        connection.ToNode!.Name.Should().Be("Small Node 1");
    }

    [Fact]
    public async Task UniqueItem_CanBeFilteredByRarity()
    {
        // Arrange
        var uniques = new[]
        {
            new UniqueItem { Name = "Common Unique", BaseType = "Sword", ItemClass = "Weapon", RarityTier = 1 },
            new UniqueItem { Name = "Rare Unique", BaseType = "Helmet", ItemClass = "Armour", RarityTier = 4 },
            new UniqueItem { Name = "Ultra Rare", BaseType = "Belt", ItemClass = "Accessory", RarityTier = 5 }
        };

        // Act
        _context.UniqueItems.AddRange(uniques);
        await _context.SaveChangesAsync();

        var highTierUniques = await _context.UniqueItems
            .Where(u => u.RarityTier >= 4)
            .OrderByDescending(u => u.RarityTier)
            .ToListAsync();

        // Assert
        highTierUniques.Should().HaveCount(2);
        highTierUniques.First().Name.Should().Be("Ultra Rare");
    }

    [Fact]
    public async Task ConcurrentOperations_AreHandledCorrectly()
    {
        // Arrange - Use a unique database name to ensure test isolation
        var tasks = new List<Task>();
        var databaseName = $"ConcurrentDb_{Guid.NewGuid()}";
        
        // Create a dedicated service provider with a shared in-memory database
        // This ensures all concurrent operations write to the same database instance
        var services = new ServiceCollection();
        services.AddDbContext<Poe2DbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Poe2DbContext>();
                
                var item = new Item
                {
                    Name = $"Item {index}",
                    BaseType = "Base",
                    ItemClass = "Class"
                };
                
                context.Items.Add(item);
                await context.SaveChangesAsync();
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Use a fresh context to verify
        using var verifyScope = serviceProvider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Poe2DbContext>();
        var count = await verifyContext.Items.CountAsync();
        count.Should().Be(10);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
