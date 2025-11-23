using Microsoft.Extensions.Logging;
using Moq;
using Poe2Mcp.Core.Models;
using Poe2Mcp.Core.Optimizers;

namespace Poe2Mcp.Tests.Optimizers;

public class PassiveOptimizerTests
{
    private readonly PassiveOptimizer _optimizer;

    public PassiveOptimizerTests()
    {
        var logger = new Mock<ILogger<PassiveOptimizer>>().Object;
        _optimizer = new PassiveOptimizer(logger);
    }

    [Fact]
    public async Task OptimizeAsync_WithAvailablePoints_ReturnsSuggestions()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int> { 1, 2, 3, 4, 5 } };

        // Act
        var result = await _optimizer.OptimizeAsync(character, availablePoints: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.AvailablePoints);
        Assert.NotEmpty(result.SuggestedAllocations);
        Assert.True(result.SuggestedAllocations.Count <= 5);
    }

    [Fact]
    public async Task OptimizeAsync_WithNoPoints_ReturnsNoAllocations()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int> { 1, 2, 3, 4, 5 } };

        // Act
        var result = await _optimizer.OptimizeAsync(character, availablePoints: 0);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.SuggestedAllocations);
    }

    [Fact]
    public async Task OptimizeAsync_WithRespecAllowed_ReturnsRespecSuggestions()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int> { 1, 2, 3, 4, 5 } };

        // Act
        var result = await _optimizer.OptimizeAsync(character, allowRespec: true, goal: OptimizationGoal.Dps);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SuggestedRespecs);
    }

    [Fact]
    public async Task OptimizeAsync_WithRespecNotAllowed_ReturnsNoRespecs()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int> { 1, 2, 3, 4, 5 } };

        // Act
        var result = await _optimizer.OptimizeAsync(character, allowRespec: false);

        // Assert
        Assert.Empty(result.SuggestedRespecs);
    }

    [Fact]
    public async Task OptimizeAsync_WithDpsGoal_SuggestsDamageNodes()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, availablePoints: 3, goal: OptimizationGoal.Dps);

        // Assert
        Assert.Equal(OptimizationGoal.Dps, result.Goal);
        Assert.NotEmpty(result.SuggestedAllocations);
        Assert.Contains(result.SuggestedAllocations, a => a.Benefit.Contains("damage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OptimizeAsync_WithDefenseGoal_SuggestsDefenseNodes()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, availablePoints: 3, goal: OptimizationGoal.Defense);

        // Assert
        Assert.Equal(OptimizationGoal.Defense, result.Goal);
        Assert.NotEmpty(result.SuggestedAllocations);
        Assert.Contains(result.SuggestedAllocations, a => a.Benefit.Contains("Life", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OptimizeAsync_GeneratesValidSummary()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, availablePoints: 5);

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
    public async Task OptimizeAsync_AllocationsHaveValidScores()
    {
        // Arrange
        var character = new CharacterData { Level = 70, Passives = new List<int>() };

        // Act
        var result = await _optimizer.OptimizeAsync(character, availablePoints: 5);

        // Assert
        foreach (var allocation in result.SuggestedAllocations)
        {
            Assert.True(allocation.Score > 0);
            Assert.NotEmpty(allocation.NodeName);
            Assert.NotEmpty(allocation.Benefit);
        }
    }
}
