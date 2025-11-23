using Microsoft.Extensions.Logging.Abstractions;
using Poe2Mcp.Core.Calculators;
using Poe2Mcp.Core.Models;
using Xunit;

namespace Poe2Mcp.Tests.Calculators;

public class StunCalculatorTests
{
    private readonly IStunCalculator _calculator;
    
    public StunCalculatorTests()
    {
        _calculator = new StunCalculator(NullLogger<StunCalculator>.Instance);
    }
    
    [Fact]
    public void CalculateLightStunChance_WithBasicHit_ReturnsCorrectChance()
    {
        // Arrange - 1000 damage to 5000 life = 20% base
        var damage = 1000.0;
        var targetMaxLife = 5000.0;
        
        // Act
        var result = _calculator.CalculateLightStunChance(
            damage,
            targetMaxLife,
            DamageType.Physical,
            AttackType.Melee);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(20.0, result.BaseChance);
        Assert.Equal(1.5, result.DamageTypeBonus); // Physical bonus
        Assert.Equal(1.5, result.AttackTypeBonus); // Melee bonus
        Assert.Equal(45.0, result.FinalChance); // 20 * 1.5 * 1.5 = 45
        Assert.True(result.WillStun); // 45% > 15% threshold
    }
    
    [Fact]
    public void CalculateLightStunChance_BelowMinimumThreshold_DoesNotStun()
    {
        // Arrange - Small damage, below threshold
        var damage = 100.0;
        var targetMaxLife = 5000.0;
        
        // Act
        var result = _calculator.CalculateLightStunChance(
            damage,
            targetMaxLife,
            DamageType.Fire,
            AttackType.Spell);
        
        // Assert
        Assert.Equal(0.0, result.FinalChance); // Below 15% threshold
        Assert.False(result.WillStun);
    }
    
    [Fact]
    public void CalculateLightStunChance_WithPhysicalDamage_AppliesBonus()
    {
        // Arrange
        var damage = 1000.0;
        var targetMaxLife = 5000.0;
        
        // Act
        var physicalResult = _calculator.CalculateLightStunChance(
            damage, targetMaxLife, DamageType.Physical, AttackType.Ranged);
        var fireResult = _calculator.CalculateLightStunChance(
            damage, targetMaxLife, DamageType.Fire, AttackType.Ranged);
        
        // Assert
        Assert.True(physicalResult.FinalChance > fireResult.FinalChance);
        Assert.Equal(1.5, physicalResult.DamageTypeBonus);
        Assert.Equal(1.0, fireResult.DamageTypeBonus);
    }
    
    [Fact]
    public void CalculateLightStunChance_WithMeleeAttack_AppliesBonus()
    {
        // Arrange
        var damage = 1000.0;
        var targetMaxLife = 5000.0;
        
        // Act
        var meleeResult = _calculator.CalculateLightStunChance(
            damage, targetMaxLife, DamageType.Physical, AttackType.Melee);
        var rangedResult = _calculator.CalculateLightStunChance(
            damage, targetMaxLife, DamageType.Physical, AttackType.Ranged);
        
        // Assert
        Assert.True(meleeResult.FinalChance > rangedResult.FinalChance);
        Assert.Equal(1.5, meleeResult.AttackTypeBonus);
        Assert.Equal(1.0, rangedResult.AttackTypeBonus);
    }
    
    [Fact]
    public void CalculateLightStunChance_WithImmunity_ReturnsZeroChance()
    {
        // Arrange
        var modifiers = new StunModifiers { ImmuneToStun = true };
        
        // Act
        var result = _calculator.CalculateLightStunChance(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            modifiers);
        
        // Assert
        Assert.Equal(0.0, result.FinalChance);
        Assert.False(result.WillStun);
    }
    
    [Fact]
    public void CalculateHeavyStunBuildup_SingleHit_AddsToMeter()
    {
        // Arrange
        var entityId = "test_entity";
        
        // Act
        var result = _calculator.CalculateHeavyStunBuildup(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
        
        // Assert
        Assert.True(result.BuildupAdded > 0);
        Assert.True(result.Meter.CurrentBuildup > 0);
        Assert.Equal(1, result.Meter.HitsReceived);
    }
    
    [Fact]
    public void CalculateHeavyStunBuildup_MultipleHits_BuildsUpMeter()
    {
        // Arrange
        var entityId = "test_entity2";
        
        // Act & Assert
        var result1 = _calculator.CalculateHeavyStunBuildup(
            800, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
        Assert.Equal(StunState.Normal, result1.Meter.State);
        
        var result2 = _calculator.CalculateHeavyStunBuildup(
            800, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
        Assert.Equal(StunState.Primed, result2.Meter.State); // Should be >50%
    }
    
    [Fact]
    public void CalculateCompleteStun_ReturnsBothLightAndHeavy()
    {
        // Arrange
        var entityId = "test_complete";
        
        // Act
        var result = _calculator.CalculateCompleteStun(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.LightStun);
        Assert.NotNull(result.HeavyStun);
        Assert.Equal(1000, result.Damage);
        Assert.Equal(5000, result.TargetMaxLife);
    }
    
    [Fact]
    public void GetHeavyStunMeter_AfterBuildup_ReturnsMeter()
    {
        // Arrange
        var entityId = "test_meter";
        _calculator.CalculateHeavyStunBuildup(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
        
        // Act
        var meter = _calculator.GetHeavyStunMeter(entityId);
        
        // Assert
        Assert.NotNull(meter);
        Assert.True(meter.CurrentBuildup > 0);
    }
    
    [Fact]
    public void ResetHeavyStunMeter_ClearsMeter()
    {
        // Arrange
        var entityId = "test_reset";
        _calculator.CalculateHeavyStunBuildup(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
        
        // Act
        _calculator.ResetHeavyStunMeter(entityId);
        var meter = _calculator.GetHeavyStunMeter(entityId);
        
        // Assert
        Assert.NotNull(meter);
        Assert.Equal(0, meter.CurrentBuildup);
        Assert.Equal(StunState.Normal, meter.State);
        Assert.Equal(0, meter.HitsReceived);
    }
    
    [Fact]
    public void RemoveEntity_RemovesFromTracking()
    {
        // Arrange
        var entityId = "test_remove";
        _calculator.CalculateHeavyStunBuildup(
            1000, 5000,
            DamageType.Physical,
            AttackType.Melee,
            entityId);
        
        // Act
        _calculator.RemoveEntity(entityId);
        var meter = _calculator.GetHeavyStunMeter(entityId);
        
        // Assert
        Assert.Null(meter);
    }
    
    [Fact]
    public void CalculateHitsToStun_ReturnsPositiveEstimates()
    {
        // Arrange
        var damagePerHit = 500.0;
        var targetMaxLife = 5000.0;
        
        // Act
        var (hitsForLight, hitsForHeavy) = _calculator.CalculateHitsToStun(
            damagePerHit,
            targetMaxLife,
            DamageType.Physical,
            AttackType.Melee);
        
        // Assert
        Assert.True(hitsForLight > 0);
        Assert.True(hitsForHeavy > 0);
    }
    
    [Fact]
    public void HeavyStunMeter_IsPrimed_ReturnsTrueWhenBetween50And100Percent()
    {
        // Arrange
        var meter = new HeavyStunMeter
        {
            CurrentBuildup = 750,
            MaxBuildup = 1000,
            BuildupPercentage = 75
        };
        meter.UpdateState();
        
        // Act & Assert
        Assert.True(meter.IsPrimed());
        Assert.Equal(StunState.Primed, meter.State);
    }
    
    [Fact]
    public void HeavyStunMeter_Reset_ClearsAllFields()
    {
        // Arrange
        var meter = new HeavyStunMeter
        {
            CurrentBuildup = 1000,
            MaxBuildup = 1000,
            BuildupPercentage = 100,
            HitsReceived = 5
        };
        meter.UpdateState();
        
        // Act
        meter.Reset();
        
        // Assert
        Assert.Equal(0, meter.CurrentBuildup);
        Assert.Equal(0, meter.BuildupPercentage);
        Assert.Equal(StunState.Normal, meter.State);
        Assert.Equal(0, meter.HitsReceived);
        Assert.Empty(meter.HitHistoryList);
    }
}
