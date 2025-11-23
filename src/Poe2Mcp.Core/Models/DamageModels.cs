namespace Poe2Mcp.Core.Models;

/// <summary>
/// Enumeration of action types for skill usage.
/// </summary>
public enum ActionType
{
    /// <summary>Attack action using attack speed</summary>
    Attack,
    
    /// <summary>Cast action using cast speed</summary>
    Cast
}

/// <summary>
/// Enumeration of modifier types.
/// </summary>
public enum ModifierType
{
    /// <summary>Additive increased modifier</summary>
    Increased,
    
    /// <summary>Multiplicative more modifier</summary>
    More,
    
    /// <summary>Negative increased (additive)</summary>
    Reduced,
    
    /// <summary>Negative more (multiplicative)</summary>
    Less
}

/// <summary>
/// Represents a damage range with minimum and maximum values.
/// </summary>
public class DamageRange
{
    /// <summary>
    /// Minimum damage value.
    /// </summary>
    public double MinDamage { get; set; }
    
    /// <summary>
    /// Maximum damage value.
    /// </summary>
    public double MaxDamage { get; set; }
    
    /// <summary>
    /// Calculate the average damage.
    /// </summary>
    public double Average() => (MinDamage + MaxDamage) / 2.0;
    
    /// <summary>
    /// Check if the damage range is valid.
    /// </summary>
    public bool IsValid() =>
        MinDamage >= 0 &&
        MaxDamage >= 0 &&
        MinDamage <= MaxDamage;
    
    /// <summary>
    /// Scale the damage range by a multiplier.
    /// </summary>
    public DamageRange Scale(double multiplier) => new()
    {
        MinDamage = MinDamage * multiplier,
        MaxDamage = MaxDamage * multiplier
    };
}

/// <summary>
/// Represents a damage modifier.
/// </summary>
public class Modifier
{
    /// <summary>
    /// Modifier value (as percentage, e.g., 50 for 50%).
    /// </summary>
    /// <remarks>
    /// Note: Value should always be positive. The sign is determined by ModifierType (Reduced/Less are treated as negative).
    /// </remarks>
    public double Value { get; set; }
    
    /// <summary>
    /// Type of modifier (increased/more/reduced/less).
    /// </summary>
    public ModifierType ModifierType { get; set; }
    
    /// <summary>
    /// Optional source description for debugging.
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Get the modifier as a decimal multiplier.
    /// </summary>
    public double GetMultiplier()
    {
        var multiplier = Value / 100.0;
        if (ModifierType is ModifierType.Reduced or ModifierType.Less)
        {
            multiplier = -multiplier;
        }
        return multiplier;
    }
}

/// <summary>
/// Represents damage broken down by type.
/// </summary>
public class DamageComponents
{
    /// <summary>
    /// Dictionary mapping damage types to damage ranges.
    /// </summary>
    public Dictionary<DamageType, DamageRange> DamageByType { get; set; } = new();
    
    /// <summary>
    /// Calculate total average damage across all types.
    /// </summary>
    public double TotalAverageDamage() =>
        DamageByType.Values.Sum(dr => dr.Average());
    
    /// <summary>
    /// Get damage range for a specific damage type.
    /// </summary>
    public DamageRange? GetDamageByType(DamageType damageType) =>
        DamageByType.GetValueOrDefault(damageType);
    
    /// <summary>
    /// Add or update damage for a specific type.
    /// </summary>
    public void AddDamage(DamageType damageType, DamageRange damageRange)
    {
        DamageByType[damageType] = DamageByType.TryGetValue(damageType, out var existing)
            ? new DamageRange
                {
                    MinDamage = existing.MinDamage + damageRange.MinDamage,
                    MaxDamage = existing.MaxDamage + damageRange.MaxDamage
                }
            : damageRange;
    }
}

/// <summary>
/// Configuration for critical strike calculations.
/// </summary>
public class CriticalStrikeConfig
{
    /// <summary>
    /// Critical strike chance (0-100).
    /// </summary>
    public double CritChance { get; set; } = 0.0;
    
    /// <summary>
    /// Critical strike damage multiplier.
    /// </summary>
    /// <remarks>
    /// In PoE2, the base critical strike multiplier is +100% (i.e., CritMultiplier = 100.0 means critical strikes deal 2x damage).
    /// This differs from PoE1, where the base multiplier is +150% (2.5x damage).
    /// </remarks>
    public double CritMultiplier { get; set; } = 100.0;
    
    /// <summary>
    /// Calculate the effective damage multiplier from critical strikes.
    /// </summary>
    /// <remarks>
    /// Formula: (1 - crit_chance) * 1.0 + crit_chance * (1 + crit_multiplier/100)
    /// </remarks>
    public double EffectiveDamageMultiplier()
    {
        var critChanceDecimal = CritChance / 100.0;
        var critDamageMultiplier = 1 + (CritMultiplier / 100.0);
        
        // Expected value calculation
        var nonCritDamage = (1 - critChanceDecimal) * 1.0;
        var critDamage = critChanceDecimal * critDamageMultiplier;
        
        return nonCritDamage + critDamage;
    }
}

/// <summary>
/// Complete DPS calculation result.
/// </summary>
public class DpsCalculationResult
{
    /// <summary>
    /// Total DPS across all damage types.
    /// </summary>
    public double TotalDps { get; set; }
    
    /// <summary>
    /// DPS broken down by damage type.
    /// </summary>
    public Dictionary<string, double> DpsByType { get; set; } = new();
    
    /// <summary>
    /// Final damage components after modifiers.
    /// </summary>
    public DamageComponents FinalDamage { get; set; } = new();
    
    /// <summary>
    /// Attacks or casts per second.
    /// </summary>
    public double ActionsPerSecond { get; set; }
}
