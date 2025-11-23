using Microsoft.EntityFrameworkCore;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Data.Models;

namespace Poe2Mcp.Tests.Data;

public class Poe2DbContextTests : IDisposable
{
    private readonly Poe2DbContext _context;

    public Poe2DbContextTests()
    {
        var options = new DbContextOptionsBuilder<Poe2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new Poe2DbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CanAddAndRetrieveItem()
    {
        // Arrange
        var item = new Item
        {
            Name = "Test Sword",
            BaseType = "One Hand Sword",
            ItemClass = "Weapon",
            RequiredLevel = 50,
            Properties = "{\"damage\":\"100-200\"}",
            IsCorrupted = false
        };

        // Act
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Items.FirstOrDefaultAsync(i => i.Name == "Test Sword");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(item.Name, retrieved.Name);
        Assert.Equal(item.BaseType, retrieved.BaseType);
        Assert.Equal(item.ItemClass, retrieved.ItemClass);
    }

    [Fact]
    public async Task CanAddAndRetrievePassiveNode()
    {
        // Arrange
        var passiveNode = new PassiveNode
        {
            NodeId = 12345,
            Name = "Test Notable",
            IsNotable = true,
            IsKeystone = false,
            Stats = "[\"10% increased damage\"]"
        };

        // Act
        _context.PassiveNodes.Add(passiveNode);
        await _context.SaveChangesAsync();

        var retrieved = await _context.PassiveNodes.FirstOrDefaultAsync(p => p.NodeId == 12345);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(passiveNode.Name, retrieved.Name);
        Assert.True(retrieved.IsNotable);
    }

    [Fact]
    public async Task CanAddAndRetrieveUniqueItem()
    {
        // Arrange
        var unique = new UniqueItem
        {
            Name = "Headhunter",
            BaseType = "Leather Belt",
            ItemClass = "Belt",
            RequiredLevel = 40,
            Modifiers = "[\"Unique mod 1\",\"Unique mod 2\"]",
            RarityTier = 5
        };

        // Act
        _context.UniqueItems.Add(unique);
        await _context.SaveChangesAsync();

        var retrieved = await _context.UniqueItems.FirstOrDefaultAsync(u => u.Name == "Headhunter");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(unique.Name, retrieved.Name);
        Assert.Equal(unique.RarityTier, retrieved.RarityTier);
    }

    [Fact]
    public async Task PassiveConnection_MaintainsRelationships()
    {
        // Arrange
        var node1 = new PassiveNode { NodeId = 1001, Name = "Node A" };
        var node2 = new PassiveNode { NodeId = 1002, Name = "Node B" };
        
        _context.PassiveNodes.AddRange(node1, node2);
        await _context.SaveChangesAsync();

        var connection = new PassiveConnection
        {
            FromNodeId = 1001,
            ToNodeId = 1002
        };

        // Act
        _context.PassiveConnections.Add(connection);
        await _context.SaveChangesAsync();

        var retrieved = await _context.PassiveConnections
            .Include(c => c.FromNode)
            .Include(c => c.ToNode)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.FromNode);
        Assert.NotNull(retrieved.ToNode);
        Assert.Equal("Node A", retrieved.FromNode.Name);
        Assert.Equal("Node B", retrieved.ToNode.Name);
    }

    [Fact]
    public async Task CanAddMultipleEntitiesOfDifferentTypes()
    {
        // Arrange
        var item = new Item { Name = "Test Item", BaseType = "Base", ItemClass = "Class" };
        var skillGem = new SkillGem { Name = "Fireball", GemType = "active" };
        var supportGem = new SupportGem { Name = "Added Fire Damage" };
        var ascendancy = new Ascendancy { Name = "Juggernaut", BaseClass = "Marauder" };

        // Act
        _context.Items.Add(item);
        _context.SkillGems.Add(skillGem);
        _context.SupportGems.Add(supportGem);
        _context.Ascendancies.Add(ascendancy);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal(1, await _context.Items.CountAsync());
        Assert.Equal(1, await _context.SkillGems.CountAsync());
        Assert.Equal(1, await _context.SupportGems.CountAsync());
        Assert.Equal(1, await _context.Ascendancies.CountAsync());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
