using Microsoft.Extensions.Logging;
using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Analyzers;

/// <summary>
/// Weakness detector for Path of Exile 2 builds.
/// Automatically identifies character weaknesses and provides actionable recommendations.
/// </summary>
public class WeaknessDetector : IWeaknessDetector
{
    private readonly ILogger<WeaknessDetector> _logger;
    
    // Recommended minimums for endgame
    private const int MIN_LIFE = 3000;
    private const int RECOMMENDED_LIFE = 5000;
    private const int MIN_ELEMENTAL_RES = 75;
    private const int MIN_CHAOS_RES = 0;
    private const int RECOMMENDED_CHAOS_RES = 50;
    /// <summary>
    /// Minimum Energy Shield to consider for hybrid build analysis.
    /// ES below this threshold is typically incidental rather than a deliberate build choice.
    /// </summary>
    private const int MIN_ES_FOR_ANALYSIS = 1000;
    
    public WeaknessDetector(ILogger<WeaknessDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("WeaknessDetector initialized");
    }
    
    public WeaknessDetectionResult DetectWeaknesses(DefensiveStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        
        _logger.LogInformation("Detecting weaknesses for character");
        
        var weaknesses = new List<Weakness>();
        
        // Detect various types of weaknesses
        weaknesses.AddRange(DetectResistanceWeaknesses(stats));
        weaknesses.AddRange(DetectLifePoolWeaknesses(stats));
        weaknesses.AddRange(DetectEnergyShieldWeaknesses(stats));
        
        // Sort by priority (descending)
        weaknesses = weaknesses.OrderByDescending(w => w.Priority).ToList();
        
        // Calculate build health score
        var buildHealthScore = CalculateBuildHealthScore(weaknesses);
        
        // Count weaknesses by severity
        var weaknessesBySeverity = new Dictionary<WeaknessSeverity, int>();
        foreach (var severity in Enum.GetValues<WeaknessSeverity>())
        {
            weaknessesBySeverity[severity] = weaknesses.Count(w => w.Severity == severity);
        }
        
        // Generate summary
        var criticalCount = weaknessesBySeverity[WeaknessSeverity.Critical];
        var highCount = weaknessesBySeverity[WeaknessSeverity.High];
        var summary = criticalCount > 0
            ? $"CRITICAL: {criticalCount} critical issues found that need immediate attention!"
            : highCount > 0
                ? $"WARNING: {highCount} high-priority issues found."
                : "Build looks healthy overall. Minor optimizations available.";
        
        _logger.LogInformation(
            "Detected {Count} weaknesses. Build health score: {Score}",
            weaknesses.Count, buildHealthScore);
        
        return new WeaknessDetectionResult
        {
            Weaknesses = weaknesses,
            BuildHealthScore = buildHealthScore,
            Summary = summary,
            WeaknessesBySeverity = weaknessesBySeverity
        };
    }
    
    public IReadOnlyList<Weakness> DetectResistanceWeaknesses(DefensiveStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        
        var weaknesses = new List<Weakness>();
        
        // Check fire resistance
        if (stats.FireResistance < MIN_ELEMENTAL_RES)
        {
            var gap = MIN_ELEMENTAL_RES - stats.FireResistance;
            weaknesses.Add(new Weakness
            {
                Category = WeaknessCategory.Resistance,
                Severity = stats.FireResistance < 0 
                    ? WeaknessSeverity.Critical 
                    : gap > 50 ? WeaknessSeverity.High : WeaknessSeverity.Medium,
                Title = "Low Fire Resistance",
                Description = "Fire resistance is below the recommended 75% cap",
                CurrentValue = $"{stats.FireResistance}%",
                RecommendedValue = $"{MIN_ELEMENTAL_RES}%",
                Impact = stats.FireResistance >= 100
                    ? "No extra fire damage taken (capped)"
                    : $"Taking {(100.0 / (100.0 - stats.FireResistance) - 1) * 100:F0}% more fire damage than necessary",
                Recommendations = new List<string>
                {
                    "Craft or find gear with fire resistance",
                    "Consider Purity of Fire aura if running reservations",
                    $"Need {gap}% more fire resistance to cap"
                },
                Priority = stats.FireResistance < 0 ? 95 : 70
            });
        }
        
        // Check cold resistance
        if (stats.ColdResistance < MIN_ELEMENTAL_RES)
        {
            var gap = MIN_ELEMENTAL_RES - stats.ColdResistance;
            weaknesses.Add(new Weakness
            {
                Category = WeaknessCategory.Resistance,
                Severity = stats.ColdResistance < 0 
                    ? WeaknessSeverity.Critical 
                    : gap > 50 ? WeaknessSeverity.High : WeaknessSeverity.Medium,
                Title = "Low Cold Resistance",
                Description = "Cold resistance is below the recommended 75% cap",
                CurrentValue = $"{stats.ColdResistance}%",
                RecommendedValue = $"{MIN_ELEMENTAL_RES}%",
                Impact = $"Vulnerable to freeze and cold damage",
                Recommendations = new List<string>
                {
                    "Craft or find gear with cold resistance",
                    "Consider Purity of Ice aura if running reservations",
                    $"Need {gap}% more cold resistance to cap"
                },
                Priority = stats.ColdResistance < 0 ? 95 : 70
            });
        }
        
        // Check lightning resistance
        if (stats.LightningResistance < MIN_ELEMENTAL_RES)
        {
            var gap = MIN_ELEMENTAL_RES - stats.LightningResistance;
            weaknesses.Add(new Weakness
            {
                Category = WeaknessCategory.Resistance,
                Severity = stats.LightningResistance < 0 
                    ? WeaknessSeverity.Critical 
                    : gap > 50 ? WeaknessSeverity.High : WeaknessSeverity.Medium,
                Title = "Low Lightning Resistance",
                Description = "Lightning resistance is below the recommended 75% cap",
                CurrentValue = $"{stats.LightningResistance}%",
                RecommendedValue = $"{MIN_ELEMENTAL_RES}%",
                Impact = $"Vulnerable to shock and lightning damage",
                Recommendations = new List<string>
                {
                    "Craft or find gear with lightning resistance",
                    "Consider Purity of Lightning aura if running reservations",
                    $"Need {gap}% more lightning resistance to cap"
                },
                Priority = stats.LightningResistance < 0 ? 95 : 70
            });
        }
        
        // Check chaos resistance (less critical but still important)
        if (stats.ChaosResistance < RECOMMENDED_CHAOS_RES)
        {
            weaknesses.Add(new Weakness
            {
                Category = WeaknessCategory.Resistance,
                Severity = stats.ChaosResistance < MIN_CHAOS_RES 
                    ? WeaknessSeverity.High 
                    : WeaknessSeverity.Medium,
                Title = "Low Chaos Resistance",
                Description = "Chaos resistance could be improved for better survivability",
                CurrentValue = $"{stats.ChaosResistance}%",
                RecommendedValue = $"{RECOMMENDED_CHAOS_RES}%",
                Impact = "More vulnerable to chaos damage and damage over time effects",
                Recommendations = new List<string>
                {
                    "Chaos resistance is optional but helpful",
                    "Consider gear with chaos resistance if available",
                    "Amethyst rings provide +15-20% chaos resistance"
                },
                Priority = stats.ChaosResistance < 0 ? 60 : 40
            });
        }
        
        return weaknesses;
    }
    
    public IReadOnlyList<Weakness> DetectLifePoolWeaknesses(DefensiveStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        
        var weaknesses = new List<Weakness>();
        
        if (stats.Life < MIN_LIFE)
        {
            weaknesses.Add(new Weakness
            {
                Category = WeaknessCategory.LifePool,
                Severity = WeaknessSeverity.Critical,
                Title = "Critically Low Life Pool",
                Description = "Life pool is dangerously low for endgame content",
                CurrentValue = $"{stats.Life}",
                RecommendedValue = $"{MIN_LIFE}+",
                Impact = "High risk of one-shots from boss attacks",
                Recommendations = new List<string>
                {
                    "Prioritize life on all gear pieces",
                    "Take life nodes on passive tree",
                    $"Need {MIN_LIFE - stats.Life} more life minimum"
                },
                Priority = 90
            });
        }
        else if (stats.Life < RECOMMENDED_LIFE)
        {
            weaknesses.Add(new Weakness
            {
                Category = WeaknessCategory.LifePool,
                Severity = WeaknessSeverity.Medium,
                Title = "Low Life Pool",
                Description = "Life pool could be improved for better survivability",
                CurrentValue = $"{stats.Life}",
                RecommendedValue = $"{RECOMMENDED_LIFE}+",
                Impact = "Moderate risk from large hits",
                Recommendations = new List<string>
                {
                    "Continue investing in life on gear",
                    "Consider additional life passive nodes",
                    $"Aim for {RECOMMENDED_LIFE - stats.Life} more life"
                },
                Priority = 60
            });
        }
        
        return weaknesses;
    }
    
    public IReadOnlyList<Weakness> DetectEnergyShieldWeaknesses(DefensiveStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        
        var weaknesses = new List<Weakness>();
        
        // Only analyze ES if it's a significant portion of the character's defense
        if (stats.EnergyShield > MIN_ES_FOR_ANALYSIS)
        {
            var totalPool = stats.Life + stats.EnergyShield;
            var esPercentage = (double)stats.EnergyShield / totalPool * 100;
            
            if (esPercentage > 50 && stats.EnergyShield < 3000)
            {
                weaknesses.Add(new Weakness
                {
                    Category = WeaknessCategory.EnergyShield,
                    Severity = WeaknessSeverity.Medium,
                    Title = "Low Energy Shield for Hybrid Build",
                    Description = "Energy Shield pool is low for a hybrid life/ES build",
                    CurrentValue = $"{stats.EnergyShield}",
                    RecommendedValue = "3000+",
                    Impact = "Not getting full value from ES investment",
                    Recommendations = new List<string>
                    {
                        "Invest in ES% on gear",
                        "Consider ES nodes on passive tree",
                        "Flat ES on gear can help significantly"
                    },
                    Priority = 50
                });
            }
        }
        
        return weaknesses;
    }
    
    private int CalculateBuildHealthScore(List<Weakness> weaknesses)
    {
        // Start at 100, deduct points based on weaknesses
        var score = 100;
        
        foreach (var weakness in weaknesses)
        {
            var deduction = weakness.Severity switch
            {
                WeaknessSeverity.Critical => 20,
                WeaknessSeverity.High => 10,
                WeaknessSeverity.Medium => 5,
                WeaknessSeverity.Low => 2,
                _ => 0
            };
            score -= deduction;
        }
        
        return Math.Max(0, score);
    }
}
