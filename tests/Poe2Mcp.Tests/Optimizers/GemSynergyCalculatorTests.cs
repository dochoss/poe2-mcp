using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Poe2Mcp.Core.Data;
using Poe2Mcp.Core.Data.Models;
using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Optimizers;

namespace Poe2Mcp.Tests.Optimizers;

public class GemSynergyCalculatorTests : IDisposable
{
    private readonly Poe2DbContext _dbContext;
    private readonly GemSynergyCalculator _calculator;

    public GemSynergyCalculatorTests()
    {
        var options = new DbContextOptionsBuilder<Poe2DbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new Poe2DbContext(options);
        
        var logger = new Mock<ILogger<GemSynergyCalculator>>().Object;
        _calculator = new GemSynergyCalculator(_dbContext, logger);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test skill gems
        _dbContext.SkillGems.AddRange(
            new SkillGem
            {
                Name = "Fireball",
                GemType = "Active",
                Tags = "[\"Spell\", \"Fire\", \"Projectile\"]",
                BaseDamage = "{\"min\": 100, \"max\": 150}",
                DamageEffectiveness = 100.0,
                CritChance = 6.0,
                AttackSpeed = 0.75,
                ManaCost = 25
            }
        );

        // Add test support gems
        _dbContext.SupportGems.AddRange(
            new Poe2Mcp.Core.Data.Models.SupportGem
            {
                Name = "Elemental Damage Support",
                Tags = "[\"Support\"]",
                CompatibleTags = "[\"Spell\"]",
                Modifiers = "{\"more_damage\": 40}",
                ManaMultiplier = 130.0
            },
            new Poe2Mcp.Core.Data.Models.SupportGem
            {
                Name = "Faster Casting Support",
                Tags = "[\"Support\"]",
                CompatibleTags = "[\"Spell\"]",
                Modifiers = "{\"increased_cast_speed\": 40}",
                ManaMultiplier = 110.0
            },
            new Poe2Mcp.Core.Data.Models.SupportGem
            {
                Name = "Spell Echo Support",
                Tags = "[\"Support\"]",
                CompatibleTags = "[\"Spell\"]",
                Modifiers = "{\"more_cast_speed\": 75, \"less_damage\": 10}",
                ManaMultiplier = 140.0
            }
        );

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task FindBestCombinationsAsync_WithValidSpell_ReturnsResults()
    {
        // Arrange
        var spellName = "Fireball";

        // Act
        var results = await _calculator.FindBestCombinationsAsync(
            spellName,
            maxSpirit: 100,
            numSupports: 2,
            topN: 5);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.True(results.Count <= 5);
    }

    [Fact]
    public async Task FindBestCombinationsAsync_WithInvalidSpell_ReturnsEmpty()
    {
        // Arrange
        var spellName = "NonExistentSpell";

        // Act
        var results = await _calculator.FindBestCombinationsAsync(spellName);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task FindBestCombinationsAsync_WithEmptySpellName_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _calculator.FindBestCombinationsAsync(""));
    }

    [Fact]
    public async Task FindBestCombinationsAsync_CalculatesValidDps()
    {
        // Arrange
        var spellName = "Fireball";

        // Act
        var results = await _calculator.FindBestCombinationsAsync(
            spellName,
            maxSpirit: 100,
            numSupports: 2,
            topN: 1);

        // Assert
        Assert.NotEmpty(results);
        var result = results.First();
        Assert.True(result.TotalDps > 0);
        Assert.True(result.AverageHit > 0);
        Assert.True(result.CastsPerSecond > 0);
    }

    [Fact]
    public async Task FindBestCombinationsAsync_WithCharacterMods_AppliesModifiers()
    {
        // Arrange
        var spellName = "Fireball";
        var characterMods = new CharacterModifiers
        {
            IncreasedDamage = 100,
            MoreDamage = 1.0
        };

        // Act
        var resultsWithMods = await _calculator.FindBestCombinationsAsync(
            spellName,
            characterMods,
            maxSpirit: 100,
            numSupports: 2,
            topN: 1);

        var resultsWithoutMods = await _calculator.FindBestCombinationsAsync(
            spellName,
            new CharacterModifiers(),
            maxSpirit: 100,
            numSupports: 2,
            topN: 1);

        // Assert
        Assert.NotEmpty(resultsWithMods);
        Assert.NotEmpty(resultsWithoutMods);
        
        // With character mods should have higher DPS
        Assert.True(resultsWithMods.First().TotalDps > resultsWithoutMods.First().TotalDps);
    }

    [Fact]
    public async Task FindBestCombinationsAsync_IncludesCalculationBreakdown()
    {
        // Arrange
        var spellName = "Fireball";

        // Act
        var results = await _calculator.FindBestCombinationsAsync(
            spellName,
            maxSpirit: 100,
            numSupports: 2,
            topN: 1);

        // Assert
        var result = results.First();
        Assert.NotEmpty(result.CalculationBreakdown);
        Assert.True(result.CalculationBreakdown.ContainsKey("base_damage"));
        Assert.True(result.CalculationBreakdown.ContainsKey("more_multiplier"));
    }

    [Fact]
    public async Task FindBestCombinationsAsync_RespectsMaxSpirit()
    {
        // Arrange
        var spellName = "Fireball";
        var maxSpirit = 50;

        // Act
        var results = await _calculator.FindBestCombinationsAsync(
            spellName,
            maxSpirit: maxSpirit,
            numSupports: 2,
            topN: 10);

        // Assert
        foreach (var result in results)
        {
            Assert.True(result.TotalSpiritCost <= maxSpirit);
        }
    }

    [Fact]
    public void FormatResult_WithResult_ReturnsFormattedString()
    {
        // Arrange
        var result = new SynergyResult
        {
            SpellName = "Fireball",
            SupportGems = new List<string> { "Support1", "Support2" },
            TotalDps = 50000,
            AverageHit = 1000,
            CastsPerSecond = 50,
            TotalSpiritCost = 45,
            TotalManaCost = 100,
            TotalMoreMultiplier = 2.0,
            DpsScore = 50000,
            EfficiencyScore = 1111.11,
            OverallScore = 60000
        };

        // Act
        var formatted = _calculator.FormatResult(result);

        // Assert
        Assert.NotEmpty(formatted);
        Assert.Contains("Fireball", formatted);
        Assert.Contains("50,000", formatted);
        Assert.Contains("Spirit Cost: 45", formatted);
    }

    [Fact]
    public void FormatResult_WithDetailedTrue_IncludesBreakdown()
    {
        // Arrange
        var result = new SynergyResult
        {
            SpellName = "Fireball",
            SupportGems = new List<string> { "Support1" },
            TotalDps = 50000,
            CalculationBreakdown = new Dictionary<string, object>
            {
                ["base_damage"] = 125.0,
                ["more_multiplier"] = 2.0
            }
        };

        // Act
        var formatted = _calculator.FormatResult(result, detailed: true);

        // Assert
        Assert.Contains("Calculation Breakdown", formatted);
        Assert.Contains("base_damage", formatted);
        Assert.Contains("more_multiplier", formatted);
    }

    [Fact]
    public void FormatResult_WithNullResult_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _calculator.FormatResult(null!));
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
