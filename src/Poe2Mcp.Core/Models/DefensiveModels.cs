namespace Poe2Mcp.Core.Models;

/// <summary>
/// Represents defensive statistics for a character
/// </summary>
public record DefensiveStats
{
    public int Life { get; init; }
    public int EnergyShield { get; init; }
    public int Armor { get; init; }
    public int Evasion { get; init; }
    public double BlockChance { get; init; }
    public int FireResistance { get; init; }
    public int ColdResistance { get; init; }
    public int LightningResistance { get; init; }
    public int ChaosResistance { get; init; }
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
