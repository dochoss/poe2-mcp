using Microsoft.Extensions.Logging;
using Moq;
using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Optimizers;

namespace Poe2Mcp.Tests.Optimizers;

public class SkillOptimizerTests
{
    private readonly SkillOptimizer _optimizer;

    public SkillOptimizerTests()
    {
        var logger = new Mock<ILogger<SkillOptimizer>>().Object;
        _optimizer = new SkillOptimizer(logger);
    }

    [Fact]
    public async Task OptimizeAsync_WithCharacter_ReturnsSuggestions()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Skills = new List<SkillData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SuggestedSetups);
    }

    [Fact]
    public async Task OptimizeAsync_WithDpsGoal_SuggestsDamageSupports()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Skills = new List<SkillData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, OptimizationGoal.Dps);

        // Assert
        Assert.Equal(OptimizationGoal.Dps, result.Goal);
        Assert.NotEmpty(result.SuggestedSetups);
        
        var setup = result.SuggestedSetups.First();
        Assert.NotEmpty(setup.SupportGems);
        Assert.True(setup.EstimatedDps > 0);
    }

    [Fact]
    public async Task OptimizeAsync_WithClearSpeedGoal_SuggestsAoeSupports()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Skills = new List<SkillData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, OptimizationGoal.ClearSpeed);

        // Assert
        Assert.Equal(OptimizationGoal.ClearSpeed, result.Goal);
        Assert.NotEmpty(result.SuggestedSetups);
        
        var setup = result.SuggestedSetups.First();
        Assert.Contains(setup.SupportGems, s => 
            s.Contains("Area", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Fork", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Chain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OptimizeAsync_WithDefenseGoal_SuggestsDefensiveSetups()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Skills = new List<SkillData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, OptimizationGoal.Defense);

        // Assert
        Assert.Equal(OptimizationGoal.Defense, result.Goal);
        Assert.NotEmpty(result.SuggestedSetups);
        
        var setup = result.SuggestedSetups.First();
        Assert.Contains("defensive", setup.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OptimizeAsync_GeneratesValidSummary()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Skills = new List<SkillData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character);

        // Assert
        Assert.NotEmpty(result.Summary);
        Assert.Contains("optimization", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OptimizeAsync_WithNullCharacter_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _optimizer.OptimizeAsync(null!));
    }

    [Fact]
    public async Task OptimizeAsync_SetupsHaveReasoning()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Skills = new List<SkillData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character);

        // Assert
        foreach (var setup in result.SuggestedSetups)
        {
            Assert.NotEmpty(setup.Reasoning);
            Assert.NotEmpty(setup.SkillName);
            Assert.NotEmpty(setup.SupportGems);
        }
    }

    [Fact]
    public async Task OptimizeAsync_IncludesSpiritCostInformation()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Skills = new List<SkillData>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character);

        // Assert
        Assert.Contains("spirit", result.Summary, StringComparison.OrdinalIgnoreCase);
        
        foreach (var setup in result.SuggestedSetups)
        {
            Assert.True(setup.TotalSpiritCost > 0);
        }
    }
}
