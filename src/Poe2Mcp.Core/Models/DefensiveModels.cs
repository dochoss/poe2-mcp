namespace Poe2Mcp.Core.Models;

/// <summary>
/// Represents defensive statistics for a character
/// </summary>
public record DefensiveStats
{
    // Core defenses
    public int Life { get; init; }
    public int EnergyShield { get; init; }
    public int Mana { get; init; }
    public int Spirit { get; init; }
    public int Armor { get; init; }
    public int Evasion { get; init; }
    
    // Attributes
    public int Strength { get; init; }
    public int Dexterity { get; init; }
    public int Intelligence { get; init; }
    
    // Block and avoidance
    public double BlockChance { get; init; }
    public double SpellBlockChance { get; init; }
    public double SpellSuppression { get; init; }
    
    // Resistances
    public int FireResistance { get; init; }
    public int ColdResistance { get; init; }
    public int LightningResistance { get; init; }
    public int ChaosResistance { get; init; }
    
    // Movement
    public double MovementSpeed { get; init; }
}

/// <summary>
/// Damage type enumeration
/// </summary>
public enum DamageType
{
    Physical,
    Fire,
    Cold,
    Lightning,
    Chaos
}

/// <summary>
/// EHP calculation result for a specific damage type
/// </summary>
public record EhpResult
{
    public DamageType DamageType { get; init; }
    public double EffectiveHealthPool { get; init; }
    public double Multiplier { get; init; }
    public string Details { get; init; } = string.Empty;
}

/// <summary>
/// Character weakness severity
/// </summary>
public enum WeaknessSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Represents a detected character weakness
/// </summary>
public record CharacterWeakness
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public WeaknessSeverity Severity { get; init; }
    public string[] Recommendations { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Threat profile for EHP calculations - defines expected hit sizes for different damage types.
/// </summary>
public class ThreatProfile
{
    /// <summary>Expected physical hit size</summary>
    public int PhysicalHitSize { get; set; } = 1000;
    
    /// <summary>Expected fire hit size</summary>
    public int FireHitSize { get; set; } = 1000;
    
    /// <summary>Expected cold hit size</summary>
    public int ColdHitSize { get; set; } = 1000;
    
    /// <summary>Expected lightning hit size</summary>
    public int LightningHitSize { get; set; } = 1000;
    
    /// <summary>Expected chaos hit size</summary>
    public int ChaosHitSize { get; set; } = 1000;
    
    /// <summary>
    /// Gets the hit size for a specific damage type.
    /// </summary>
    public int GetHitSize(DamageType damageType) => damageType switch
    {
        DamageType.Physical => PhysicalHitSize,
        DamageType.Fire => FireHitSize,
        DamageType.Cold => ColdHitSize,
        DamageType.Lightning => LightningHitSize,
        DamageType.Chaos => ChaosHitSize,
        _ => 1000
    };
}
