using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Data.Models;
using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Optimizers;

namespace Poe2Mcp.Tests.Optimizers;

public class GearOptimizerTests : IDisposable
{
    private readonly Poe2DbContext _dbContext;
    private readonly GearOptimizer _optimizer;

    public GearOptimizerTests()
    {
        var options = new DbContextOptionsBuilder<Poe2DbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new Poe2DbContext(options);
        
        var logger = new Mock<ILogger<GearOptimizer>>().Object;
        _optimizer = new GearOptimizer(_dbContext, logger);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add some test unique items
        _dbContext.UniqueItems.AddRange(
            new UniqueItem
            {
                Name = "Tabula Rasa",
                ItemClass = "Body Armour",
                RequiredLevel = 1,
                Stats = "{\"chaos_value\": 5.0, \"life\": 0, \"resistances\": 0}"
            },
            new UniqueItem
            {
                Name = "Kaom's Heart",
                ItemClass = "Body Armour",
                RequiredLevel = 68,
                Stats = "{\"chaos_value\": 150.0, \"life\": 500, \"resistances\": 0}"
            },
            new UniqueItem
            {
                Name = "Goldrim",
                ItemClass = "Helmet",
                RequiredLevel = 1,
                Stats = "{\"chaos_value\": 1.0, \"resistances\": 120}"
            },
            new UniqueItem
            {
                Name = "Starkonja's Head",
                ItemClass = "Helmet",
                RequiredLevel = 60,
                Stats = "{\"chaos_value\": 30.0, \"life\": 100, \"attack_speed\": 10}"
            }
        );

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task OptimizeAsync_WithEmptyCharacter_ReturnsResult()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Items = new List<ItemData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, BudgetTier.Medium, OptimizationGoal.Balanced);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BudgetTier.Medium, result.BudgetTier);
        Assert.Equal(OptimizationGoal.Balanced, result.Goal);
    }

    [Fact]
    public async Task OptimizeAsync_WithLowBudget_RespectsLimit()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Items = new List<ItemData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, BudgetTier.Low, OptimizationGoal.Defense);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalEstimatedCostChaos <= 10 || result.PriorityUpgrades.Count == 0);
    }

    [Fact]
    public async Task OptimizeAsync_GeneratesValidSummary()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Items = new List<ItemData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, BudgetTier.Medium, OptimizationGoal.Defense);

        // Assert
        Assert.NotEmpty(result.Summary);
        Assert.Contains("optimization", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OptimizeAsync_WithNullCharacterData_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _optimizer.OptimizeAsync(null!, BudgetTier.Medium, OptimizationGoal.Balanced));
    }

    [Fact]
    public async Task OptimizeAsync_SortsByPriority()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Items = new List<ItemData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, BudgetTier.Medium, OptimizationGoal.Balanced);

        // Assert
        if (result.PriorityUpgrades.Count > 1)
        {
            // Verify sorting: items are sorted descending by priority
            for (int i = 1; i < result.PriorityUpgrades.Count; i++)
            {
                var prevPriority = (int)result.PriorityUpgrades[i - 1].Priority;
                var currPriority = (int)result.PriorityUpgrades[i].Priority;
                Assert.True(prevPriority >= currPriority);
            }
        }
    }

    [Fact]
    public async Task OptimizeAsync_IncludesReasoningForUpgrades()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Items = new List<ItemData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, BudgetTier.Medium, OptimizationGoal.Balanced);

        // Assert
        foreach (var upgrade in result.PriorityUpgrades)
        {
            Assert.NotEmpty(upgrade.Reasoning);
        }
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
