namespace Poe2Mcp.Core.Calculators;

using Poe2Mcp.Core.Models;

/// <summary>
/// Calculator for Effective Health Pool (EHP) based on PoE2 mechanics
/// </summary>
public class EhpCalculator : IEhpCalculator
{
    /// <summary>
    /// Calculate EHP against all damage types and return as dictionary.
    /// </summary>
    public Dictionary<string, double> CalculateEhp(
        DefensiveStats stats,
        ThreatProfile? threatProfile = null)
    {
        threatProfile ??= new ThreatProfile();
        var result = new Dictionary<string, double>();

        foreach (DamageType damageType in Enum.GetValues<DamageType>())
        {
            var hitSize = threatProfile.GetHitSize(damageType);
            var ehp = CalculateEhpForDamageType(stats, damageType, hitSize);
            result[damageType.ToString()] = ehp.EffectiveHealthPool;
        }

        return result;
    }

    /// <summary>
    /// Calculate EHP against all damage types with detailed results.
    /// </summary>
    public IReadOnlyList<EhpResult> CalculateEhpDetailed(DefensiveStats stats, int expectedHitSize = 1000)
    {
        var results = new List<EhpResult>();
        
        // Calculate for each damage type
        foreach (DamageType damageType in Enum.GetValues<DamageType>())
        {
            var ehp = CalculateEhpForDamageType(stats, damageType, expectedHitSize);
            results.Add(ehp);
        }
        
        return results;
    }

    private EhpResult CalculateEhpForDamageType(
        DefensiveStats stats, 
        DamageType damageType, 
        int expectedHitSize)
    {
        var rawPool = stats.Life + stats.EnergyShield;
        var resistance = GetResistance(stats, damageType);
        
        // Calculate damage mitigation from resistance (capped at 75%)
        var cappedResistance = Math.Min(resistance, 75);
        var resistMultiplier = 100.0 / (100.0 - cappedResistance);
        
        // Calculate armor mitigation for physical damage
        double armorMultiplier = 1.0;
        if (damageType == DamageType.Physical && stats.Armor > 0)
        {
            // PoE2 armor formula: Reduction = Armor / (Armor + 10 * Damage)
            var reduction = stats.Armor / (double)(stats.Armor + 10 * expectedHitSize);
            armorMultiplier = 1.0 / (1.0 - reduction);
        }
        
        // Chaos damage deals double damage to ES in PoE2
        double esMultiplier = 1.0;
        if (damageType == DamageType.Chaos)
        {
            var effectiveEs = stats.EnergyShield / 2.0; // Chaos does 2x damage to ES
            rawPool = stats.Life + (int)effectiveEs;
        }
        
        // Calculate total multiplier
        var totalMultiplier = resistMultiplier * armorMultiplier * esMultiplier;
        var ehp = rawPool * totalMultiplier;
        
        return new EhpResult
        {
            DamageType = damageType,
            EffectiveHealthPool = ehp,
            Multiplier = totalMultiplier,
            Details = $"Raw Pool: {rawPool}, Resist: {cappedResistance}%, Multiplier: {totalMultiplier:F2}x"
        };
    }

    private static int GetResistance(DefensiveStats stats, DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Fire => stats.FireResistance,
            DamageType.Cold => stats.ColdResistance,
            DamageType.Lightning => stats.LightningResistance,
            DamageType.Chaos => stats.ChaosResistance,
            DamageType.Physical => 0,
            _ => 0
        };
    }
}
