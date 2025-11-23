using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Calculators;

/// <summary>
/// Main damage calculator for Path of Exile 2.
/// Handles all damage calculations including base damage, modifiers,
/// critical strikes, attack speed, and DPS calculations.
/// </summary>
public class DamageCalculator : IDamageCalculator
{
    private readonly ILogger<DamageCalculator> _logger;
    
    public DamageCalculator(ILogger<DamageCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("DamageCalculator initialized");
    }
    
    public DamageComponents CalculateBaseDamage(
        DamageRange? weaponDamage = null,
        DamageRange? spellBaseDamage = null,
        IEnumerable<(DamageType Type, DamageRange Range)>? addedFlatDamage = null)
    {
        if (weaponDamage == null && spellBaseDamage == null)
        {
            throw new ArgumentException("Must provide either weaponDamage or spellBaseDamage");
        }
        
        var components = new DamageComponents();
        
        // Start with weapon or spell base damage (assumed physical for weapons)
        if (weaponDamage != null)
        {
            components.AddDamage(DamageType.Physical, weaponDamage);
            _logger.LogDebug("Base weapon damage: {Min}-{Max}", 
                weaponDamage.MinDamage, weaponDamage.MaxDamage);
        }
        else if (spellBaseDamage != null)
        {
            components.AddDamage(DamageType.Physical, spellBaseDamage);
            _logger.LogDebug("Base spell damage: {Min}-{Max}", 
                spellBaseDamage.MinDamage, spellBaseDamage.MaxDamage);
        }
        
        // Add flat damage bonuses
        if (addedFlatDamage != null)
        {
            foreach (var (damageType, damageRange) in addedFlatDamage)
            {
                components.AddDamage(damageType, damageRange);
                _logger.LogDebug("Added {Type} damage: {Min}-{Max}", 
                    damageType, damageRange.MinDamage, damageRange.MaxDamage);
            }
        }
        
        return components;
    }
    
    public double ApplyIncreasedModifiers(double baseValue, IEnumerable<Modifier> modifiers)
    {
        // Filter only increased/reduced modifiers
        var relevantMods = modifiers
            .Where(m => m.ModifierType is ModifierType.Increased or ModifierType.Reduced)
            .ToList();
        
        // Sum all increased/reduced modifiers
        var totalIncreased = relevantMods.Sum(mod => mod.GetMultiplier());
        
        var result = baseValue * (1 + totalIncreased);
        
        if (relevantMods.Count > 0)
        {
            _logger.LogDebug(
                "Applied {Count} increased modifiers: {Base} * (1 + {Total:F2}) = {Result:F2}",
                relevantMods.Count, baseValue, totalIncreased, result);
        }
        
        return result;
    }
    
    public double ApplyMoreModifiers(double baseValue, IEnumerable<Modifier> modifiers)
    {
        // Filter only more/less modifiers
        var relevantMods = modifiers
            .Where(m => m.ModifierType is ModifierType.More or ModifierType.Less)
            .ToList();
        
        // Apply each more/less modifier multiplicatively
        var result = baseValue;
        var multipliers = relevantMods.Select(mod => 1 + mod.GetMultiplier()).ToList();
        
        foreach (var multiplier in multipliers)
        {
            result *= multiplier;
        }
        
        if (relevantMods.Count > 0)
        {
            _logger.LogDebug(
                "Applied {Count} more modifiers: {Base} * {Multipliers} = {Result:F2}",
                relevantMods.Count, baseValue, 
                string.Join(" * ", multipliers.Select(m => $"{m:F2}")), result);
        }
        
        return result;
    }
    
    public DamageComponents ApplyDamageConversion(
        DamageComponents components,
        Dictionary<DamageType, Dictionary<DamageType, double>> conversions)
    {
        var result = new DamageComponents();
        
        // Process each damage type
        foreach (var (sourceType, damageRange) in components.DamageByType)
        {
            var remainingPercentage = 100.0;
            
            // Apply conversions from this source type
            if (conversions.TryGetValue(sourceType, out var sourceConversions))
            {
                foreach (var (targetType, conversionPct) in sourceConversions)
                {
                    if (conversionPct > 0)
                    {
                        // Convert portion to target type
                        var convertedDamage = damageRange.Scale(conversionPct / 100.0);
                        result.AddDamage(targetType, convertedDamage);
                        remainingPercentage -= conversionPct;
                        
                        _logger.LogDebug(
                            "Converted {Pct}% of {Source} to {Target}",
                            conversionPct, sourceType, targetType);
                    }
                }
            }
            
            // Add remaining unconverted damage
            if (remainingPercentage > 0)
            {
                var remainingDamage = damageRange.Scale(remainingPercentage / 100.0);
                result.AddDamage(sourceType, remainingDamage);
            }
        }
        
        return result;
    }
    
    public DamageRange CalculateFinalDamage(
        DamageRange baseDamage,
        IEnumerable<Modifier>? increasedModifiers = null,
        IEnumerable<Modifier>? moreModifiers = null)
    {
        if (!baseDamage.IsValid())
            throw new ArgumentException("Invalid damage range", nameof(baseDamage));
        increasedModifiers ??= Array.Empty<Modifier>();
        moreModifiers ??= Array.Empty<Modifier>();
        
        // Apply increased modifiers to min and max
        var minAfterIncreased = ApplyIncreasedModifiers(
            baseDamage.MinDamage,
            increasedModifiers);
        var maxAfterIncreased = ApplyIncreasedModifiers(
            baseDamage.MaxDamage,
            increasedModifiers);
        
        // Apply more modifiers to min and max
        var finalMin = ApplyMoreModifiers(minAfterIncreased, moreModifiers);
        var finalMax = ApplyMoreModifiers(maxAfterIncreased, moreModifiers);
        
        return new DamageRange
        {
            MinDamage = finalMin,
            MaxDamage = finalMax
        };
    }
    
    public DamageRange CalculateCriticalDamage(
        DamageRange baseDamage,
        CriticalStrikeConfig critConfig)
    {
        ArgumentNullException.ThrowIfNull(critConfig);
        
        var multiplier = 1 + (critConfig.CritMultiplier / 100.0);
        
        _logger.LogDebug(
            "Critical strike multiplier: {Mult}% (total: {Total:F2}x)",
            critConfig.CritMultiplier, multiplier);
        
        return baseDamage.Scale(multiplier);
    }
    
    public double CalculateAttackSpeed(
        double baseAttackTime,
        IEnumerable<Modifier> increasedSpeedModifiers)
    {
        if (baseAttackTime <= 0)
        {
            throw new ArgumentException(
                $"Base attack time must be positive: {baseAttackTime}",
                nameof(baseAttackTime));
        }
        
        // Calculate base attacks per second
        var baseAps = 1 / baseAttackTime;
        
        // Apply increased attack speed modifiers
        var finalAps = ApplyIncreasedModifiers(baseAps, increasedSpeedModifiers);
        
        _logger.LogDebug(
            "Attack speed: {Base}s base -> {Final:F2} attacks/sec",
            baseAttackTime, finalAps);
        
        return finalAps;
    }
    
    public double CalculateCastSpeed(
        double baseCastTime,
        IEnumerable<Modifier> increasedSpeedModifiers)
    {
        if (baseCastTime <= 0)
        {
            throw new ArgumentException(
                $"Base cast time must be positive: {baseCastTime}",
                nameof(baseCastTime));
        }
        
        // Calculate base casts per second
        var baseCps = 1 / baseCastTime;
        
        // Apply increased cast speed modifiers
        var finalCps = ApplyIncreasedModifiers(baseCps, increasedSpeedModifiers);
        
        _logger.LogDebug(
            "Cast speed: {Base}s base -> {Final:F2} casts/sec",
            baseCastTime, finalCps);
        
        return finalCps;
    }
    
    public double CalculateDps(
        DamageRange damagePerHit,
        double actionsPerSecond,
        CriticalStrikeConfig? critConfig = null)
    {
        ArgumentNullException.ThrowIfNull(damagePerHit);
        
        var avgDamage = damagePerHit.Average();
        
        // Apply critical strike multiplier if provided
        if (critConfig != null)
        {
            var critMultiplier = critConfig.EffectiveDamageMultiplier();
            avgDamage *= critMultiplier;
            _logger.LogDebug("Applied crit multiplier: {Mult:F2}x", critMultiplier);
        }
        
        var dps = avgDamage * actionsPerSecond;
        
        _logger.LogInformation(
            "DPS calculation: {AvgDmg:F2} avg damage * {Aps:F2} aps = {Dps:F2} DPS",
            damagePerHit.Average(), actionsPerSecond, dps);
        
        return dps;
    }
    
    public DpsCalculationResult CalculateFullDps(
        DamageComponents baseDamageComponents,
        IEnumerable<Modifier>? increasedDamageModifiers = null,
        IEnumerable<Modifier>? moreDamageModifiers = null,
        double baseActionTime = 1.0,
        IEnumerable<Modifier>? increasedSpeedModifiers = null,
        CriticalStrikeConfig? critConfig = null,
        bool isSpell = false)
    {
        ArgumentNullException.ThrowIfNull(baseDamageComponents);
        
        increasedDamageModifiers ??= Array.Empty<Modifier>();
        moreDamageModifiers ??= Array.Empty<Modifier>();
        increasedSpeedModifiers ??= Array.Empty<Modifier>();
        
        // Calculate action speed
        var actionsPerSecond = isSpell
            ? CalculateCastSpeed(baseActionTime, increasedSpeedModifiers)
            : CalculateAttackSpeed(baseActionTime, increasedSpeedModifiers);
        
        // Process each damage type
        var finalComponents = new DamageComponents();
        var dpsByType = new Dictionary<string, double>();
        var totalDps = 0.0;
        
        foreach (var (damageType, damageRange) in baseDamageComponents.DamageByType)
        {
            // Apply damage modifiers
            var finalDamage = CalculateFinalDamage(
                damageRange,
                increasedDamageModifiers,
                moreDamageModifiers);
            
            finalComponents.AddDamage(damageType, finalDamage);
            
            // Calculate DPS for this damage type
            var typeDps = CalculateDps(finalDamage, actionsPerSecond, critConfig);
            dpsByType[damageType.ToString()] = typeDps;
            totalDps += typeDps;
            
            _logger.LogDebug("{Type} DPS: {Dps:F2}", damageType, typeDps);
        }
        
        _logger.LogInformation("Total DPS: {TotalDps:F2}", totalDps);
        
        return new DpsCalculationResult
        {
            TotalDps = totalDps,
            DpsByType = dpsByType,
            FinalDamage = finalComponents,
            ActionsPerSecond = actionsPerSecond
        };
    }
}
