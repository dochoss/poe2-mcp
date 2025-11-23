using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Categories of build weaknesses.
/// </summary>
public enum WeaknessCategory
{
    Resistance,
    LifePool,
    EnergyShield,
    DefenseLayers,
    Spirit,
    OvercappedStat,
    Damage,
    ResourceManagement,
    StunVulnerability
}

/// <summary>
/// Represents a detected character weakness.
/// </summary>
public class Weakness
{
    /// <summary>
    /// Category of weakness.
    /// </summary>
    public WeaknessCategory Category { get; set; }
    
    /// <summary>
    /// Severity level.
    /// </summary>
    public WeaknessSeverity Severity { get; set; }
    
    /// <summary>
    /// Short title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Current value of the stat.
    /// </summary>
    public string CurrentValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Recommended target value.
    /// </summary>
    public string RecommendedValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Impact description.
    /// </summary>
    public string Impact { get; set; } = string.Empty;
    
    /// <summary>
    /// List of specific recommendations to fix.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Priority score (0-100, higher = more urgent).
    /// </summary>
    public int Priority { get; set; } = 50;
}

/// <summary>
/// Result of weakness detection analysis.
/// </summary>
public class WeaknessDetectionResult
{
    /// <summary>
    /// List of detected weaknesses.
    /// </summary>
    public List<Weakness> Weaknesses { get; set; } = new();
    
    /// <summary>
    /// Overall build health score (0-100).
    /// </summary>
    public int BuildHealthScore { get; set; }
    
    /// <summary>
    /// Summary of critical issues.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of weaknesses by severity.
    /// </summary>
    public Dictionary<WeaknessSeverity, int> WeaknessesBySeverity { get; set; } = new();
}

/// <summary>
/// Interface for weakness detector.
/// </summary>
public interface IWeaknessDetector
{
    /// <summary>
    /// Detect weaknesses in a character's build.
    /// </summary>
    WeaknessDetectionResult DetectWeaknesses(DefensiveStats stats);
    
    /// <summary>
    /// Detect resistance weaknesses.
    /// </summary>
    IReadOnlyList<Weakness> DetectResistanceWeaknesses(DefensiveStats stats);
    
    /// <summary>
    /// Detect life pool weaknesses.
    /// </summary>
    IReadOnlyList<Weakness> DetectLifePoolWeaknesses(DefensiveStats stats);
    
    /// <summary>
    /// Detect energy shield weaknesses.
    /// </summary>
    IReadOnlyList<Weakness> DetectEnergyShieldWeaknesses(DefensiveStats stats);
}
