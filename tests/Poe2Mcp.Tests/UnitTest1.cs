using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Tests;

public class EhpCalculatorTests
{
    private readonly EhpCalculator _calculator = new();

    [Fact]
    public void CalculateEhp_WithBasicStats_ReturnsValidResults()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5000,
            EnergyShield = 0,
            Armor = 0,
            Evasion = 0,
            BlockChance = 0,
            FireResistance = 75,
            ColdResistance = 75,
            LightningResistance = 75,
            ChaosResistance = 0
        };

        // Act
        var results = _calculator.CalculateEhpDetailed(stats);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(5, results.Count); // One for each damage type
        
        // Fire/Cold/Lightning EHP should be 4x raw pool (75% resist = 4x multiplier)
        var fireEhp = results.First(r => r.DamageType == DamageType.Fire);
        Assert.Equal(20000, fireEhp.EffectiveHealthPool, precision: 0);
        
        // Physical and Chaos should equal raw pool (no mitigation)
        var physicalEhp = results.First(r => r.DamageType == DamageType.Physical);
        Assert.Equal(5000, physicalEhp.EffectiveHealthPool, precision: 0);
    }

    [Fact]
    public void CalculateEhp_WithArmor_MitigatesPhysicalDamage()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 5000,
            EnergyShield = 0,
            Armor = 10000,
            Evasion = 0,
            BlockChance = 0,
            FireResistance = 0,
            ColdResistance = 0,
            LightningResistance = 0,
            ChaosResistance = 0
        };

        // Act
        var results = _calculator.CalculateEhpDetailed(stats, expectedHitSize: 1000);

        // Assert
        var physicalEhp = results.First(r => r.DamageType == DamageType.Physical);
        
        // With 10000 armor vs 1000 hit: reduction = 10000 / (10000 + 10*1000) = 0.5
        // EHP multiplier = 1 / (1 - 0.5) = 2x
        Assert.True(physicalEhp.EffectiveHealthPool > 5000);
        Assert.InRange(physicalEhp.EffectiveHealthPool, 9000, 11000);
    }

    [Fact]
    public void CalculateEhp_WithChaosAndES_AppliesDoubledamagePenalty()
    {
        // Arrange
        var stats = new DefensiveStats
        {
            Life = 3000,
            EnergyShield = 2000,
            Armor = 0,
            Evasion = 0,
            BlockChance = 0,
            FireResistance = 0,
            ColdResistance = 0,
            LightningResistance = 0,
            ChaosResistance = 0
        };

        // Act
        var results = _calculator.CalculateEhpDetailed(stats);

        // Assert
        var chaosEhp = results.First(r => r.DamageType == DamageType.Chaos);
        var physicalEhp = results.First(r => r.DamageType == DamageType.Physical);
        
        // Chaos does 2x damage to ES, so effective pool = 3000 + 2000/2 = 4000
        Assert.Equal(4000, chaosEhp.EffectiveHealthPool, precision: 0);
        
        // Physical uses full pool
        Assert.Equal(5000, physicalEhp.EffectiveHealthPool, precision: 0);
    }
}
