using Microsoft.Extensions.Logging;
using Moq;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;
using Xunit;

namespace Poe2Mcp.Tests.Calculators;

public class SpiritCalculatorTests
{
    private readonly ISpiritCalculator _calculator;
    private readonly Mock<ILogger<SpiritCalculator>> _loggerMock;

    public SpiritCalculatorTests()
    {
        _loggerMock = new Mock<ILogger<SpiritCalculator>>();
        _calculator = new SpiritCalculator(_loggerMock.Object);
    }

    [Fact]
    public void AddQuestSpirit_AddsToMaximumSpirit()
    {
        // Arrange & Act
        _calculator.AddQuestSpirit("First Skull Quest", 30);
        _calculator.AddQuestSpirit("Second Skull Quest", 30);
        _calculator.AddQuestSpirit("Third Skull Quest", 40);

        // Assert
        Assert.Equal(100, _calculator.GetMaximumSpirit());
        Assert.Equal(100, _calculator.GetSpiritBySourceType(SpiritSourceType.Quest));
    }

    [Fact]
    public void AddGearSpirit_AddsToMaximumSpirit()
    {
        // Arrange & Act
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddGearSpirit("Helmet", 30);
        _calculator.AddGearSpirit("Body Armour", 20);

        // Assert
        Assert.Equal(150, _calculator.GetMaximumSpirit());
        Assert.Equal(50, _calculator.GetSpiritBySourceType(SpiritSourceType.Gear));
    }

    [Fact]
    public void AddPassiveSpirit_AddsToMaximumSpirit()
    {
        // Arrange & Act
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddPassiveSpirit("Minion Master Node", 15);
        _calculator.AddPassiveSpirit("Spirit Reserve Node", 10);

        // Assert
        Assert.Equal(125, _calculator.GetMaximumSpirit());
        Assert.Equal(25, _calculator.GetSpiritBySourceType(SpiritSourceType.PassiveTree));
    }

    [Fact]
    public void AddReservation_WithNoSupports_CalculatesBaseCost()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);

        // Act
        var reservation = _calculator.AddReservation(
            "Purity of Fire",
            30,
            SpiritReservationType.Aura);

        // Assert
        Assert.Equal(30, reservation.CalculateCost());
        Assert.Equal(30, _calculator.GetSpiritReserved());
        Assert.Equal(70, _calculator.GetSpiritAvailable());
    }

    [Fact]
    public void AddReservation_WithSupports_CalculatesMultipliedCost()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);

        // Act
        var reservation = _calculator.AddReservation(
            "Raise Zombie",
            25,
            SpiritReservationType.PermanentMinion,
            new[] { ("Minion Damage", 1.5), ("Minion Life", 1.3) });

        // Assert
        // 25 * 1.5 * 1.3 = 48.75, rounds up to 49
        Assert.Equal(49, reservation.CalculateCost());
        Assert.Equal(49, _calculator.GetSpiritReserved());
    }

    [Fact]
    public void IsOverflowing_WithTooManyReservations_ReturnsTrue()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("Aura1", 40, SpiritReservationType.Aura);
        _calculator.AddReservation("Aura2", 40, SpiritReservationType.Aura);
        _calculator.AddReservation("Aura3", 40, SpiritReservationType.Aura);

        // Act & Assert
        Assert.True(_calculator.IsOverflowing());
        Assert.Equal(20, _calculator.GetOverflowAmount());
        Assert.Equal(-20, _calculator.GetSpiritAvailable());
    }

    [Fact]
    public void ToggleReservation_DisablesAndEnables()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("Aura", 30, SpiritReservationType.Aura);
        var initialReserved = _calculator.GetSpiritReserved();

        // Act - Disable
        var disabledState = _calculator.ToggleReservation("Aura");

        // Assert - Disabled
        Assert.False(disabledState);
        Assert.Equal(0, _calculator.GetSpiritReserved());

        // Act - Enable
        var enabledState = _calculator.ToggleReservation("Aura");

        // Assert - Enabled
        Assert.True(enabledState);
        Assert.Equal(initialReserved, _calculator.GetSpiritReserved());
    }

    [Fact]
    public void RemoveReservation_RemovesFromTracking()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("Aura", 30, SpiritReservationType.Aura);

        // Act
        var removed = _calculator.RemoveReservation("Aura");

        // Assert
        Assert.True(removed);
        Assert.Equal(0, _calculator.GetSpiritReserved());
        Assert.Null(_calculator.GetReservation("Aura"));
    }

    [Fact]
    public void GetSpiritSummary_ReturnsCompleteStatus()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddGearSpirit("Helmet", 30);
        _calculator.AddReservation("Aura", 40, SpiritReservationType.Aura);
        _calculator.AddReservation("Minion", 25, SpiritReservationType.PermanentMinion);

        // Act
        var summary = _calculator.GetSpiritSummary();

        // Assert
        Assert.Equal(130, summary.MaximumSpirit);
        Assert.Equal(65, summary.ReservedSpirit);
        Assert.Equal(65, summary.AvailableSpirit);
        Assert.False(summary.IsOverflowing);
        Assert.Equal(0, summary.OverflowAmount);
        Assert.Equal(50.0, summary.UtilizationPercent, 0.1);
        Assert.Equal(2, summary.ActiveReservations);
        Assert.Equal(2, summary.TotalReservations);
    }

    [Fact]
    public void GetOptimizationSuggestions_WithOverflow_ReturnsSuggestions()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("LowPriority", 40, SpiritReservationType.Aura, priority: 8);
        _calculator.AddReservation("HighPriority", 40, SpiritReservationType.Aura, priority: 2);
        _calculator.AddReservation("MediumPriority", 40, SpiritReservationType.Aura, priority: 5);

        // Act
        var suggestions = _calculator.GetOptimizationSuggestions();

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Equal("LowPriority", suggestions.First().Target); // Highest priority value = least important
    }

    [Fact]
    public void AutoResolveOverflow_DisablesLowPriorityReservations()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("Essential", 30, SpiritReservationType.Aura, priority: 1);
        _calculator.AddReservation("Optional", 40, SpiritReservationType.Aura, priority: 8);
        _calculator.AddReservation("Nice", 50, SpiritReservationType.Aura, priority: 6);

        // Act
        var actions = _calculator.AutoResolveOverflow();

        // Assert
        Assert.NotEmpty(actions);
        Assert.False(_calculator.IsOverflowing());
        Assert.True(_calculator.GetReservation("Essential")!.Enabled);
    }

    [Fact]
    public void SuggestOptimalConfiguration_MaximizesEfficiency()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("Important", 40, SpiritReservationType.Aura, priority: 1);
        _calculator.AddReservation("Medium", 35, SpiritReservationType.Aura, priority: 5);
        _calculator.AddReservation("Low", 30, SpiritReservationType.Aura, priority: 9);

        // Act
        var optimal = _calculator.SuggestOptimalConfiguration();

        // Assert
        Assert.Equal(100, optimal.MaximumSpirit);
        Assert.Contains("Important", optimal.EnabledReservations);
        Assert.Contains("Medium", optimal.EnabledReservations);
        Assert.Equal(75, optimal.OptimalSpiritUsed);
        Assert.Equal(25, optimal.OptimalSpiritRemaining);
    }

    [Fact]
    public void ValidateConfiguration_WithOverflow_ReturnsInvalid()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("Aura1", 60, SpiritReservationType.Aura);
        _calculator.AddReservation("Aura2", 60, SpiritReservationType.Aura);

        // Act
        var (isValid, issues) = _calculator.ValidateConfiguration();

        // Assert
        Assert.False(isValid);
        Assert.Contains(issues, i => i.Contains("overflow"));
    }

    [Fact]
    public void ValidateConfiguration_WithNoSources_ReturnsInvalid()
    {
        // Arrange
        // No sources added

        // Act
        var (isValid, issues) = _calculator.ValidateConfiguration();

        // Assert
        Assert.False(isValid);
        Assert.Contains(issues, i => i.Contains("No Spirit sources configured"));
    }

    [Fact]
    public void ValidateConfiguration_WithDuplicateNames_ReturnsInvalid()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        _calculator.AddReservation("Duplicate", 20, SpiritReservationType.Aura);
        _calculator.AddReservation("Duplicate", 30, SpiritReservationType.Aura);

        // Act
        var (isValid, issues) = _calculator.ValidateConfiguration();

        // Assert
        Assert.False(isValid);
        Assert.Contains(issues, i => i.Contains("Duplicate reservation names"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(75, 75)]
    [InlineData(50, 50)]
    public void GetSpiritBySourceType_FiltersCorrectly(int questAmount, int gearAmount)
    {
        // Arrange
        if (questAmount > 0)
            _calculator.AddQuestSpirit("Quest", questAmount);
        if (gearAmount > 0)
            _calculator.AddGearSpirit("Gear", gearAmount);

        // Act & Assert
        Assert.Equal(questAmount, _calculator.GetSpiritBySourceType(SpiritSourceType.Quest));
        Assert.Equal(gearAmount, _calculator.GetSpiritBySourceType(SpiritSourceType.Gear));
    }

    [Fact]
    public void ReservationCalculateCost_RoundsUpCorrectly()
    {
        // Arrange
        _calculator.AddQuestSpirit("Quest", 100);
        
        // Act
        var reservation = _calculator.AddReservation(
            "Test",
            20,
            SpiritReservationType.Aura,
            new[] { ("Support", 1.5), ("Support2", 1.3) });

        // Assert
        // 20 * 1.5 * 1.3 = 39, no rounding needed
        Assert.Equal(39, reservation.CalculateCost());
        
        // Test with fractional result
        var reservation2 = _calculator.AddReservation(
            "Test2",
            21,
            SpiritReservationType.Aura,
            new[] { ("Support", 1.5) });
        
        // 21 * 1.5 = 31.5, rounds up to 32
        Assert.Equal(32, reservation2.CalculateCost());
    }
}
