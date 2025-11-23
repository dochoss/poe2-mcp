using Microsoft.Extensions.Logging.Abstractions;
using Poe2Mcp.Core.Analyzers;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;
using Xunit;

namespace Poe2Mcp.Tests.Analyzers;

public class GearEvaluatorTests
{
    private readonly IGearEvaluator _evaluator;
    private readonly IEhpCalculator _ehpCalculator;

    public GearEvaluatorTests()
    {
        _ehpCalculator = new EhpCalculator();
        _evaluator = new GearEvaluator(
            NullLogger<GearEvaluator>.Instance,
            _ehpCalculator);
    }

    [Fact]
    public void EvaluateUpgrade_WithBetterHelmet_ReturnsStrongUpgrade()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "Basic Helmet",
            ItemSlot = "Helmet",
            Life = 50,
            FireRes = 30,
            Armor = 200
        };

        var upgradeGear = new GearStats
        {
            ItemName = "Better Helmet",
            ItemSlot = "Helmet",
            Life = 80,
            FireRes = 45,
            ColdRes = 35,
            Armor = 300
        };

        var baseStats = new CharacterStats
        {
            Life = 3000,
            FireRes = 45,
            ColdRes = 40,
            Armor = 1000
        };

        // Act
        var result = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PriorityScore > 50, "Priority score should be > 50 for a clear upgrade");
        Assert.True(
            result.Recommendation is UpgradeRecommendation.Upgrade or UpgradeRecommendation.StrongUpgrade,
            "Should recommend upgrade");
        
        // Check stat changes
        Assert.Equal(30, result.StatChanges["life"]);
        Assert.Equal(15, result.ResistanceChanges["fire"]);
        Assert.Equal(35, result.ResistanceChanges["cold"]);
    }

    [Fact]
    public void EvaluateUpgrade_WithDowngrade_ReturnsSkipOrDowngrade()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "Good Helmet",
            ItemSlot = "Helmet",
            Life = 100,
            FireRes = 45,
            ColdRes = 40,
            Armor = 500
        };

        var upgradeGear = new GearStats
        {
            ItemName = "Worse Helmet",
            ItemSlot = "Helmet",
            Life = 60,
            FireRes = 20,
            Armor = 300
        };

        var baseStats = new CharacterStats
        {
            Life = 4000,
            FireRes = 30,
            ColdRes = 35,
            Armor = 2000
        };

        // Act
        var result = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PriorityScore < 50, "Priority score should be < 50 for a downgrade");
        Assert.True(
            result.Recommendation is UpgradeRecommendation.Skip or UpgradeRecommendation.Downgrade,
            "Should recommend skip or downgrade");
        
        // Should have warnings about losses
        Assert.True(result.Warnings.Count > 0, "Should have warnings");
        Assert.Contains(result.Warnings, w => w.Contains("resistance"));
    }

    [Fact]
    public void EvaluateUpgrade_WithResistanceImprovementBelowCap_GivesHighPriority()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "No Res Helmet",
            ItemSlot = "Helmet",
            Life = 80,
            Armor = 300
        };

        var upgradeGear = new GearStats
        {
            ItemName = "High Res Helmet",
            ItemSlot = "Helmet",
            Life = 80,
            FireRes = 40,
            ColdRes = 40,
            LightningRes = 40,
            Armor = 300
        };

        var baseStats = new CharacterStats
        {
            Life = 4000,
            FireRes = 35,  // Below cap - improvement is valuable
            ColdRes = 35,
            LightningRes = 35,
            Armor = 2000
        };

        // Act
        var result = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PriorityScore > 60, "Resistance improvements below cap should have high priority");
        Assert.Equal(UpgradeRecommendation.StrongUpgrade, result.Recommendation);
    }

    [Fact]
    public void EvaluateMultipleUpgrades_RanksByPriorityScore()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "Basic Helmet",
            ItemSlot = "Helmet",
            Life = 50,
            Armor = 200
        };

        var potentialUpgrades = new List<(GearStats Gear, double? Price)>
        {
            (new GearStats
            {
                ItemName = "Mediocre Helmet",
                ItemSlot = "Helmet",
                Life = 60,
                Armor = 250
            }, 10.0),
            (new GearStats
            {
                ItemName = "Great Helmet",
                ItemSlot = "Helmet",
                Life = 100,
                FireRes = 40,
                ColdRes = 40,
                Armor = 400
            }, 50.0),
            (new GearStats
            {
                ItemName = "Good Helmet",
                ItemSlot = "Helmet",
                Life = 80,
                FireRes = 30,
                Armor = 350
            }, 25.0)
        };

        var baseStats = new CharacterStats
        {
            Life = 3000,
            FireRes = 35,
            ColdRes = 35,
            Armor = 1000
        };

        // Act
        var results = _evaluator.EvaluateMultipleUpgrades(
            currentGear,
            potentialUpgrades,
            baseStats,
            topN: 3);

        // Assert
        Assert.Equal(3, results.Count);
        
        // Results should be sorted by priority score descending
        Assert.True(results[0].Value.PriorityScore >= results[1].Value.PriorityScore);
        Assert.True(results[1].Value.PriorityScore >= results[2].Value.PriorityScore);
        
        // Best item should be the "Great Helmet"
        Assert.Equal("Great Helmet", results[0].Gear.ItemName);
    }

    [Fact]
    public void CompareItems_IdentifiesWinner()
    {
        // Arrange
        var itemA = new GearStats
        {
            ItemName = "Helmet A",
            ItemSlot = "Helmet",
            Life = 80,
            FireRes = 40,
            Armor = 300
        };

        var itemB = new GearStats
        {
            ItemName = "Helmet B",
            ItemSlot = "Helmet",
            Life = 70,
            FireRes = 30,
            ColdRes = 35,
            Armor = 350
        };

        var baseStats = new CharacterStats
        {
            Life = 4000,
            FireRes = 35,
            ColdRes = 40,
            Armor = 2000
        };

        // Act
        var comparison = _evaluator.CompareItems(itemA, itemB, baseStats);

        // Assert
        Assert.NotNull(comparison);
        Assert.Equal("Helmet A", comparison.ItemA);
        Assert.Equal("Helmet B", comparison.ItemB);
        Assert.NotEmpty(comparison.Winner);
        Assert.True(comparison.ScoreDifference >= 0);
    }

    [Fact]
    public void EvaluateUpgrade_WithSpiritGain_IncreasesScore()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "Basic Amulet",
            ItemSlot = "Amulet",
            Life = 50
        };

        var upgradeGear = new GearStats
        {
            ItemName = "Spirit Amulet",
            ItemSlot = "Amulet",
            Life = 50,
            Spirit = 20
        };

        var baseStats = new CharacterStats
        {
            Life = 4000,
            Spirit = 80
        };

        // Act
        var result = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.StatChanges["spirit"]);
        Assert.True(result.PriorityScore > 50, "Spirit gain should increase priority score");
    }

    [Fact]
    public void EvaluateUpgrade_WithLifeLoss_GeneratesWarning()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "Life Helmet",
            ItemSlot = "Helmet",
            Life = 150,
            Armor = 300
        };

        var upgradeGear = new GearStats
        {
            ItemName = "No Life Helmet",
            ItemSlot = "Helmet",
            FireRes = 45,
            Armor = 500
        };

        var baseStats = new CharacterStats
        {
            Life = 3000,
            FireRes = 30,
            Armor = 1000
        };

        // Act
        var result = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Warnings, w => w.Contains("life") && w.Contains("Loses"));
    }

    [Fact]
    public void EvaluateUpgrade_WithPriceConsidered_AdjustsScore()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "Basic Helmet",
            ItemSlot = "Helmet",
            Life = 50,
            Armor = 200
        };

        var upgradeGear = new GearStats
        {
            ItemName = "Slightly Better Helmet",
            ItemSlot = "Helmet",
            Life = 60,
            Armor = 220
        };

        var baseStats = new CharacterStats
        {
            Life = 4000,
            Armor = 2000
        };

        // Act - without price
        var resultNoPrice = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats);
        
        // Act - with very high price
        var resultHighPrice = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats, priceChaos: 1000.0);

        // Assert
        Assert.True(resultHighPrice.PriorityScore <= resultNoPrice.PriorityScore,
            "High price should reduce or maintain priority score");
        Assert.Equal(1000.0, resultHighPrice.TradeValue);
    }

    [Fact]
    public void EvaluateUpgrade_CalculatesEhpChanges()
    {
        // Arrange
        var currentGear = new GearStats
        {
            ItemName = "Low Res Helmet",
            ItemSlot = "Helmet",
            Life = 60,
            FireRes = 10,
            Armor = 250
        };

        var upgradeGear = new GearStats
        {
            ItemName = "High Res Helmet",
            ItemSlot = "Helmet",
            Life = 60,
            FireRes = 40,
            Armor = 250
        };

        var baseStats = new CharacterStats
        {
            Life = 4000,
            FireRes = 35,  // Current total: 45, Upgraded total: 75
            Armor = 2000
        };

        // Act
        var result = _evaluator.EvaluateUpgrade(currentGear, upgradeGear, baseStats);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.EhpChanges);
        
        // Fire EHP should increase significantly (going from 45% to 75% res)
        if (result.EhpChanges.TryGetValue("Fire", out var fireChanges))
        {
            Assert.True(fireChanges["percent"] > 0, "Fire EHP should increase");
        }
    }
}
