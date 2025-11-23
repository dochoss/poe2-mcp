using Microsoft.Extensions.Logging.Abstractions;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;
using Xunit;

namespace Poe2Mcp.Tests.Calculators;

public class DamageCalculatorTests
{
    private readonly IDamageCalculator _calculator;
    
    public DamageCalculatorTests()
    {
        _calculator = new DamageCalculator(NullLogger<DamageCalculator>.Instance);
    }
    
    [Fact]
    public void CalculateBaseDamage_WithWeaponDamage_ReturnsPhysicalDamage()
    {
        // Arrange
        var weaponDamage = new DamageRange { MinDamage = 50, MaxDamage = 100 };
        
        // Act
        var result = _calculator.CalculateBaseDamage(weaponDamage: weaponDamage);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.DamageByType.ContainsKey(DamageType.Physical));
        Assert.Equal(50, result.DamageByType[DamageType.Physical].MinDamage);
        Assert.Equal(100, result.DamageByType[DamageType.Physical].MaxDamage);
    }
    
    [Fact]
    public void ApplyIncreasedModifiers_WithMultipleModifiers_SumsAdditively()
    {
        // Arrange
        var modifiers = new[]
        {
            new Modifier { Value = 50, ModifierType = ModifierType.Increased },
            new Modifier { Value = 30, ModifierType = ModifierType.Increased },
            new Modifier { Value = 10, ModifierType = ModifierType.Reduced }
        };
        
        // Act
        var result = _calculator.ApplyIncreasedModifiers(100, modifiers);
        
        // Assert
        Assert.Equal(170.0, result, 2); // 100 * (1 + 0.5 + 0.3 - 0.1)
    }
    
    [Fact]
    public void ApplyMoreModifiers_WithMultipleModifiers_AppliesMultiplicatively()
    {
        // Arrange
        var modifiers = new[]
        {
            new Modifier { Value = 30, ModifierType = ModifierType.More },
            new Modifier { Value = 20, ModifierType = ModifierType.More },
            new Modifier { Value = 10, ModifierType = ModifierType.Less }
        };
        
        // Act
        var result = _calculator.ApplyMoreModifiers(100, modifiers);
        
        // Assert
        // 100 * 1.3 * 1.2 * 0.9 = 140.4
        Assert.InRange(result, 140.3, 140.5);
    }
    
    [Fact]
    public void CalculateFinalDamage_WithBothModifierTypes_AppliesInCorrectOrder()
    {
        // Arrange
        var baseDamage = new DamageRange { MinDamage = 100, MaxDamage = 200 };
        var increased = new[]
        {
            new Modifier { Value = 50, ModifierType = ModifierType.Increased }
        };
        var more = new[]
        {
            new Modifier { Value = 30, ModifierType = ModifierType.More }
        };
        
        // Act
        var result = _calculator.CalculateFinalDamage(baseDamage, increased, more);
        
        // Assert
        // Min: 100 * 1.5 * 1.3 = 195
        // Max: 200 * 1.5 * 1.3 = 390
        Assert.Equal(195.0, result.MinDamage, 2);
        Assert.Equal(390.0, result.MaxDamage, 2);
    }
    
    [Fact]
    public void ApplyDamageConversion_Converts50PercentPhysicalToFire()
    {
        // Arrange
        var components = new DamageComponents();
        components.AddDamage(DamageType.Physical, new DamageRange { MinDamage = 100, MaxDamage = 200 });
        
        var conversions = new Dictionary<DamageType, Dictionary<DamageType, double>>
        {
            {
                DamageType.Physical, new Dictionary<DamageType, double>
                {
                    { DamageType.Fire, 50 }
                }
            }
        };
        
        // Act
        var result = _calculator.ApplyDamageConversion(components, conversions);
        
        // Assert
        Assert.Equal(2, result.DamageByType.Count);
        Assert.Equal(75, result.GetDamageByType(DamageType.Physical)!.Average());
        Assert.Equal(75, result.GetDamageByType(DamageType.Fire)!.Average());
    }
    
    [Fact]
    public void CalculateAttackSpeed_WithIncreasedSpeed_CalculatesCorrectly()
    {
        // Arrange
        var baseAttackTime = 1.5;
        var modifiers = new[]
        {
            new Modifier { Value = 20, ModifierType = ModifierType.Increased }
        };
        
        // Act
        var result = _calculator.CalculateAttackSpeed(baseAttackTime, modifiers);
        
        // Assert
        // (1 / 1.5) * 1.2 = 0.8 attacks per second
        Assert.InRange(result, 0.79, 0.81);
    }
    
    [Fact]
    public void CalculateDps_WithCritConfig_AppliesCritMultiplier()
    {
        // Arrange
        var damage = new DamageRange { MinDamage = 100, MaxDamage = 200 };
        var critConfig = new CriticalStrikeConfig { CritChance = 50, CritMultiplier = 100 };
        
        // Act
        var result = _calculator.CalculateDps(damage, 2.0, critConfig);
        
        // Assert
        // Average damage: 150
        // Crit multiplier: (0.5 * 1) + (0.5 * 2) = 1.5
        // DPS: 150 * 1.5 * 2 = 450
        Assert.InRange(result, 449, 451);
    }
    
    [Fact]
    public void CalculateFullDps_WithCompleteSetup_ReturnsValidResult()
    {
        // Arrange
        var baseComponents = new DamageComponents();
        baseComponents.AddDamage(DamageType.Physical, new DamageRange { MinDamage = 100, MaxDamage = 200 });
        
        var increasedDamage = new[]
        {
            new Modifier { Value = 100, ModifierType = ModifierType.Increased }
        };
        var moreDamage = new[]
        {
            new Modifier { Value = 50, ModifierType = ModifierType.More }
        };
        
        // Act
        var result = _calculator.CalculateFullDps(
            baseComponents,
            increasedDamage,
            moreDamage,
            baseActionTime: 1.0);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalDps > 0);
        Assert.True(result.ActionsPerSecond > 0);
        Assert.True(result.DpsByType.ContainsKey("Physical"));
    }
    
    [Fact]
    public void DamageRange_Average_CalculatesCorrectly()
    {
        // Arrange
        var range = new DamageRange { MinDamage = 100, MaxDamage = 200 };
        
        // Act
        var average = range.Average();
        
        // Assert
        Assert.Equal(150, average);
    }
    
    [Fact]
    public void DamageRange_Scale_MultipliesCorrectly()
    {
        // Arrange
        var range = new DamageRange { MinDamage = 10, MaxDamage = 20 };
        
        // Act
        var scaled = range.Scale(2.0);
        
        // Assert
        Assert.Equal(20, scaled.MinDamage);
        Assert.Equal(40, scaled.MaxDamage);
    }
}
