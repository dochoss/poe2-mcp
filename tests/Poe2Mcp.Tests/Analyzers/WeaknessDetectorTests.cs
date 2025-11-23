using Microsoft.Extensions.Logging.Abstractions;
using Poe2Mcp.Core.Analyzers;
using Poe2Mcp.Core.Models;
using Xunit;

namespace Poe2Mcp.Tests.Analyzers;

public class WeaknessDetectorTests
{
    private readonly IWeaknessDetector _detector;
    
    public WeaknessDetectorTests()
    {
        _detector = new WeaknessDetector(NullLogger<WeaknessDetector>.Instance);
    }
    
    [Fact]
    public void DetectWeaknesses_WithPerfectStats_ReturnsHighScore()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5000,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 50
        };
        
        // Act
        var result = _detector.DetectWeaknesses(stats);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.BuildHealthScore >= 90);
        Assert.Equal(0, result.WeaknessesBySeverity[WeaknessSeverity.Critical]);
    }
    
    [Fact]
    public void DetectWeaknesses_WithLowLife_DetectsCriticalWeakness()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 2000, // Below minimum
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 0
        };
        
        // Act
        var result = _detector.DetectWeaknesses(stats);
        
        // Assert
        Assert.True(result.Weaknesses.Count > 0);
        Assert.Contains(result.Weaknesses, w => 
            w.Category == WeaknessCategory.LifePool && 
            w.Severity == WeaknessSeverity.Critical);
    }
    
    [Fact]
    public void DetectResistanceWeaknesses_WithNegativeResistance_ReturnsCritical()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5000,
            FireResistance = -20, // Negative!
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 0
        };
        
        // Act
        var weaknesses = _detector.DetectResistanceWeaknesses(stats);
        
        // Assert
        Assert.NotEmpty(weaknesses);
        Assert.Contains(weaknesses, w =>
            w.Category == WeaknessCategory.Resistance &&
            w.Severity == WeaknessSeverity.Critical &&
            w.Title.Contains("Fire"));
    }
    
    [Fact]
    public void DetectResistanceWeaknesses_WithCappedResistances_ReturnsMinimal()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5000,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 50
        };
        
        // Act
        var weaknesses = _detector.DetectResistanceWeaknesses(stats);
        
        // Assert
        // Should be empty or only have low-priority chaos warning
        Assert.True(weaknesses.Count <= 1);
    }
    
    [Fact]
    public void DetectLifePoolWeaknesses_WithAdequateLife_ReturnsEmpty()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5500 // Above recommended
        };
        
        // Act
        var weaknesses = _detector.DetectLifePoolWeaknesses(stats);
        
        // Assert
        Assert.Empty(weaknesses);
    }
    
    [Fact]
    public void DetectLifePoolWeaknesses_WithLowLife_ReturnsMediumSeverity()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 3500 // Between min and recommended
        };
        
        // Act
        var weaknesses = _detector.DetectLifePoolWeaknesses(stats);
        
        // Assert
        Assert.NotEmpty(weaknesses);
        Assert.Contains(weaknesses, w => w.Severity == WeaknessSeverity.Medium);
    }
    
    [Fact]
    public void DetectEnergyShieldWeaknesses_WithLowESHybridBuild_DetectsIssue()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 1500,
            EnergyShield = 2500 // More than 50% ES but low total (under 3000 minimum)
        };
        
        // Act
        var weaknesses = _detector.DetectEnergyShieldWeaknesses(stats);
        
        // Assert
        Assert.NotEmpty(weaknesses);
        Assert.Contains(weaknesses, w => w.Category == WeaknessCategory.EnergyShield);
    }
    
    [Fact]
    public void DetectWeaknesses_WithMultipleIssues_SortsByPriority()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 2000, // Critical
            FireResistance = 50, // Medium
            ColdResistance = -10, // Critical
            LightningResistance = 75,
            ChaosResistance = 0
        };
        
        // Act
        var result = _detector.DetectWeaknesses(stats);
        
        // Assert
        Assert.True(result.Weaknesses.Count >= 2);
        // Should be sorted by priority (descending)
        for (int i = 0; i < result.Weaknesses.Count - 1; i++)
        {
            Assert.True(result.Weaknesses[i].Priority >= result.Weaknesses[i + 1].Priority);
        }
    }
    
    [Fact]
    public void DetectWeaknesses_CalculatesWeaknessesBySeverity()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 2000,
            FireResistance = -10,
            ColdResistance = 60,
            LightningResistance = 75,
            ChaosResistance = 0
        };
        
        // Act
        var result = _detector.DetectWeaknesses(stats);
        
        // Assert
        Assert.NotNull(result.WeaknessesBySeverity);
        var totalCounted = result.WeaknessesBySeverity.Values.Sum();
        Assert.Equal(result.Weaknesses.Count, totalCounted);
    }
}
