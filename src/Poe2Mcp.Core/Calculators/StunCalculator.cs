using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Calculators;

/// <summary>
/// Calculator for Path of Exile 2 stun mechanics.
/// Handles both Light Stun (chance-based) and Heavy Stun (buildup-based)
/// calculations with support for damage type and attack type bonuses.
/// </summary>
public class StunCalculator : IStunCalculator
{
    private readonly ILogger<StunCalculator> _logger;
    private readonly Dictionary<string, HeavyStunMeter> _entityMeters = new();
    
    // Constants
    private const double LIGHT_STUN_MINIMUM_THRESHOLD = 15.0;  // Minimum 15% chance required
    private const double PHYSICAL_DAMAGE_BONUS = 1.5;  // 50% more stun for physical damage
    private const double MELEE_ATTACK_BONUS = 1.5;  // 50% more stun for melee attacks
    private const double PRIMED_STATE_THRESHOLD = 50.0;  // Primed at 50% meter
    private const double HEAVY_STUN_THRESHOLD = 100.0;  // Heavy Stun at 100% meter
    private const double HEAVY_STUN_DURATION = 3.0;  // 3 seconds Heavy Stun duration
    
    public StunCalculator(ILogger<StunCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("StunCalculator initialized");
    }
    
    public LightStunResult CalculateLightStunChance(
        double damage,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        StunModifiers? modifiers = null)
    {
        if (damage < 0)
            throw new ArgumentException($"Damage cannot be negative: {damage}", nameof(damage));
        if (targetMaxLife <= 0)
            throw new ArgumentException($"Target max life must be positive: {targetMaxLife}", nameof(targetMaxLife));
        
        modifiers ??= new StunModifiers();
        
        // Immunity check
        if (modifiers.ImmuneToStun)
        {
            _logger.LogDebug("Target is immune to stun");
            return new LightStunResult
            {
                BaseChance = 0.0,
                DamageTypeBonus = 1.0,
                AttackTypeBonus = 1.0,
                FinalChance = 0.0,
                WillStun = false,
                Damage = damage,
                TargetMaxLife = targetMaxLife
            };
        }
        
        // Calculate base chance
        var baseChance = (damage / targetMaxLife) * 100.0;
        
        // Apply damage type bonus
        var damageTypeBonus = damageType == DamageType.Physical ? PHYSICAL_DAMAGE_BONUS : 1.0;
        
        // Apply attack type bonus
        var attackTypeBonus = attackType == AttackType.Melee ? MELEE_ATTACK_BONUS : 1.0;
        
        // Calculate chance with bonuses
        var chance = baseChance * damageTypeBonus * attackTypeBonus;
        
        // Apply increased modifier (additive)
        if (modifiers.IncreasedStunChance != 0)
        {
            chance *= (1.0 + modifiers.IncreasedStunChance / 100.0);
        }
        
        // Apply more modifier (multiplicative)
        chance *= modifiers.MoreStunChance;
        
        // Apply stun threshold modifiers (affects the "effective damage")
        var thresholdMultiplier = modifiers.IncreasedStunThreshold * modifiers.ReducedStunThreshold;
        if (thresholdMultiplier != 1.0)
        {
            // Inverse relationship: lower threshold = higher chance
            chance /= thresholdMultiplier;
        }
        
        // Cap at 100%
        var finalChance = Math.Min(chance, 100.0);
        
        // Check minimum threshold
        var minimumThreshold = modifiers.MinimumStunChance ?? LIGHT_STUN_MINIMUM_THRESHOLD;
        
        var willStun = finalChance >= minimumThreshold;
        
        var result = new LightStunResult
        {
            BaseChance = baseChance,
            DamageTypeBonus = damageTypeBonus,
            AttackTypeBonus = attackTypeBonus,
            FinalChance = finalChance,
            WillStun = willStun,
            Damage = damage,
            TargetMaxLife = targetMaxLife
        };
        
        _logger.LogDebug("Light Stun calculated: {Result}", result);
        return result;
    }
    
    public HeavyStunResult CalculateHeavyStunBuildup(
        double damage,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        string entityId = "default",
        StunModifiers? modifiers = null,
        bool lightStunWouldOccur = false)
    {
        if (damage < 0)
            throw new ArgumentException($"Damage cannot be negative: {damage}", nameof(damage));
        if (targetMaxLife <= 0)
            throw new ArgumentException($"Target max life must be positive: {targetMaxLife}", nameof(targetMaxLife));
        
        modifiers ??= new StunModifiers();
        
        // Get or create meter for this entity
        if (!_entityMeters.ContainsKey(entityId))
        {
            _entityMeters[entityId] = new HeavyStunMeter
            {
                MaxBuildup = targetMaxLife
            };
            _logger.LogDebug("Created Heavy Stun meter for entity: {EntityId}", entityId);
        }
        
        var meter = _entityMeters[entityId];
        
        // Update max buildup if target life changed
        if (meter.MaxBuildup != targetMaxLife)
        {
            var oldPercentage = meter.BuildupPercentage;
            meter.MaxBuildup = targetMaxLife;
            meter.BuildupPercentage = meter.MaxBuildup > 0 
                ? (meter.CurrentBuildup / meter.MaxBuildup * 100.0) 
                : 0.0;
            _logger.LogDebug(
                "Updated max buildup for {EntityId}: {Old:F1}% -> {New:F1}%",
                entityId, oldPercentage, meter.BuildupPercentage);
        }
        
        // Check if currently Heavy Stunned
        var wasHeavyStunned = meter.IsHeavyStunned();
        
        // Immunity check
        if (modifiers.ImmuneToStun)
        {
            _logger.LogDebug("Entity {EntityId} is immune to stun", entityId);
            return new HeavyStunResult
            {
                BuildupAdded = 0.0,
                TotalBuildup = meter.CurrentBuildup,
                Meter = meter,
                TriggeredHeavyStun = false,
                TriggeredCrushingBlow = false,
                HitsToHeavyStun = double.PositiveInfinity
            };
        }
        
        // Check Primed state before adding buildup (for Crushing Blow)
        var wasPrimed = meter.IsPrimed();
        
        // Calculate buildup (same formula as Light Stun chance, but as raw value)
        var baseBuildup = damage;
        
        // Apply damage type bonus
        var damageTypeBonus = damageType == DamageType.Physical ? PHYSICAL_DAMAGE_BONUS : 1.0;
        
        // Apply attack type bonus
        var attackTypeBonus = attackType == AttackType.Melee ? MELEE_ATTACK_BONUS : 1.0;
        
        // Calculate buildup with bonuses
        var buildup = baseBuildup * damageTypeBonus * attackTypeBonus;
        
        // Apply increased modifier (additive)
        if (modifiers.IncreasedStunChance != 0)
        {
            buildup *= (1.0 + modifiers.IncreasedStunChance / 100.0);
        }
        
        // Apply more modifier (multiplicative)
        buildup *= modifiers.MoreStunChance;
        
        // Apply buildup multiplier
        buildup *= modifiers.StunBuildupMultiplier;
        
        // Apply stun threshold modifiers
        var thresholdMultiplier = modifiers.IncreasedStunThreshold * modifiers.ReducedStunThreshold;
        // Use epsilon comparison for floating-point equality
        if (Math.Abs(thresholdMultiplier - 1.0) > 1e-6)
        {
            buildup /= thresholdMultiplier;
        }
        
        // Add to meter
        meter.CurrentBuildup += buildup;
        meter.BuildupPercentage = meter.MaxBuildup > 0 
            ? (meter.CurrentBuildup / meter.MaxBuildup * 100.0) 
            : 0.0;
        meter.HitsReceived++;
        meter.HitHistoryList.Add(new HitHistory
        {
            Damage = damage,
            Buildup = buildup,
            TotalBuildup = meter.CurrentBuildup,
            Percentage = meter.BuildupPercentage
        });
        
        // Update state
        meter.UpdateState();
        
        // Check for Heavy Stun trigger
        var triggeredHeavyStun = !wasHeavyStunned && meter.IsHeavyStunned();
        
        // Check for Crushing Blow (was primed, Light Stun would occur)
        var triggeredCrushingBlow = wasPrimed && lightStunWouldOccur;
        
        // Calculate hits needed to Heavy Stun
        var remainingBuildup = meter.MaxBuildup - meter.CurrentBuildup;
        double hitsToHeavyStun;
        if (buildup > 0 && remainingBuildup > 0)
        {
            hitsToHeavyStun = remainingBuildup / buildup;
        }
        else if (meter.IsHeavyStunned())
        {
            hitsToHeavyStun = 0.0;
        }
        else
        {
            hitsToHeavyStun = double.PositiveInfinity;
        }
        
        var result = new HeavyStunResult
        {
            BuildupAdded = buildup,
            TotalBuildup = meter.CurrentBuildup,
            Meter = meter,
            TriggeredHeavyStun = triggeredHeavyStun,
            TriggeredCrushingBlow = triggeredCrushingBlow,
            HitsToHeavyStun = hitsToHeavyStun
        };
        
        if (triggeredHeavyStun)
            _logger.LogInformation("Heavy Stun triggered on entity {EntityId}!", entityId);
        if (triggeredCrushingBlow)
            _logger.LogInformation("Crushing Blow triggered on entity {EntityId}!", entityId);
        
        _logger.LogDebug("Heavy Stun buildup calculated: {Result}", result);
        return result;
    }
    
    public CompleteStunResult CalculateCompleteStun(
        double damage,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        string entityId = "default",
        StunModifiers? modifiers = null)
    {
        // Calculate Light Stun
        var lightStun = CalculateLightStunChance(
            damage,
            targetMaxLife,
            damageType,
            attackType,
            modifiers);
        
        // Calculate Heavy Stun with Light Stun result
        var heavyStun = CalculateHeavyStunBuildup(
            damage,
            targetMaxLife,
            damageType,
            attackType,
            entityId,
            modifiers,
            lightStun.WillStun);
        
        var result = new CompleteStunResult
        {
            LightStun = lightStun,
            HeavyStun = heavyStun,
            Damage = damage,
            TargetMaxLife = targetMaxLife,
            DamageType = damageType,
            AttackType = attackType
        };
        
        _logger.LogDebug("Complete stun calculated for entity {EntityId}", entityId);
        return result;
    }
    
    public HeavyStunMeter? GetHeavyStunMeter(string entityId = "default")
    {
        return _entityMeters.GetValueOrDefault(entityId);
    }
    
    public void ResetHeavyStunMeter(string entityId = "default")
    {
        if (_entityMeters.TryGetValue(entityId, out var meter))
        {
            meter.Reset();
            _logger.LogInformation("Reset Heavy Stun meter for entity: {EntityId}", entityId);
        }
        else
        {
            _logger.LogWarning("No meter found for entity: {EntityId}", entityId);
        }
    }
    
    public void RemoveEntity(string entityId)
    {
        if (_entityMeters.Remove(entityId))
        {
            _logger.LogInformation("Removed entity from tracking: {EntityId}", entityId);
        }
        else
        {
            _logger.LogWarning("No meter found for entity: {EntityId}", entityId);
        }
    }
    
    public IReadOnlyList<string> GetAllTrackedEntities()
    {
        return _entityMeters.Keys.ToList();
    }
    
    public (double HitsForLightStun, double HitsForHeavyStun) CalculateHitsToStun(
        double damagePerHit,
        double targetMaxLife,
        DamageType damageType,
        AttackType attackType,
        StunModifiers? modifiers = null)
    {
        modifiers ??= new StunModifiers();
        
        // Calculate Light Stun chance for one hit
        var lightResult = CalculateLightStunChance(
            damagePerHit,
            targetMaxLife,
            damageType,
            attackType,
            modifiers);
        
        // Calculate hits needed to reach 15% threshold
        double hitsForLight;
        if (lightResult.BaseChance == 0)
        {
            hitsForLight = double.PositiveInfinity;
        }
        else
        {
            var chancePerHit = lightResult.BaseChance * 
                               lightResult.DamageTypeBonus * 
                               lightResult.AttackTypeBonus;
            
            if (modifiers.IncreasedStunChance != 0)
            {
                chancePerHit *= (1.0 + modifiers.IncreasedStunChance / 100.0);
            }
            chancePerHit *= modifiers.MoreStunChance;
            
            var minimumThreshold = modifiers.MinimumStunChance ?? LIGHT_STUN_MINIMUM_THRESHOLD;
            
            if (chancePerHit >= minimumThreshold)
            {
                hitsForLight = 1.0;
            }
            else if (lightResult.BaseChance > 0)
            {
                hitsForLight = minimumThreshold / chancePerHit;
            }
            else
            {
                hitsForLight = double.PositiveInfinity;
            }
        }
        
        // Calculate Heavy Stun buildup per hit
        var baseBuildup = damagePerHit;
        var damageTypeBonus = damageType == DamageType.Physical ? PHYSICAL_DAMAGE_BONUS : 1.0;
        var attackTypeBonus = attackType == AttackType.Melee ? MELEE_ATTACK_BONUS : 1.0;
        
        var buildupPerHit = baseBuildup * damageTypeBonus * attackTypeBonus;
        
        if (modifiers.IncreasedStunChance != 0)
        {
            buildupPerHit *= (1.0 + modifiers.IncreasedStunChance / 100.0);
        }
        buildupPerHit *= modifiers.MoreStunChance;
        buildupPerHit *= modifiers.StunBuildupMultiplier;
        
        var thresholdMultiplier = modifiers.IncreasedStunThreshold * modifiers.ReducedStunThreshold;
        if (thresholdMultiplier != 1.0)
        {
            buildupPerHit /= thresholdMultiplier;
        }
        
        // Calculate hits for Heavy Stun
        var hitsForHeavy = buildupPerHit > 0 
            ? targetMaxLife / buildupPerHit 
            : double.PositiveInfinity;
        
        _logger.LogDebug(
            "Hits to stun: Light={HitsLight:F2}, Heavy={HitsHeavy:F2}",
            hitsForLight, hitsForHeavy);
        
        return (hitsForLight, hitsForHeavy);
    }
}
