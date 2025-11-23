namespace Poe2Mcp.Core.Models;

/// <summary>
/// Budget tier for gear optimization
/// </summary>
public enum BudgetTier
{
    Low,
    Medium,
    High,
    Unlimited
}

/// <summary>
/// Optimization goal for various optimizers
/// </summary>
public enum OptimizationGoal
{
    Dps,
    Defense,
    Balanced,
    BossDamage,
    ClearSpeed,
    Efficiency,
    Utility
}

/// <summary>
/// Priority level for upgrade recommendations
/// </summary>
public enum UpgradePriority
{
    None,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Gear slot names
/// </summary>
public enum GearSlot
{
    Helmet,
    BodyArmour,
    Gloves,
    Boots,
    Weapon,
    Offhand,
    Amulet,
    Ring,
    Belt
}

/// <summary>
/// Result of gear optimization analysis
/// </summary>
public class GearOptimizationResult
{
    public List<GearUpgradeRecommendation> PriorityUpgrades { get; set; } = new();
    public BudgetTier BudgetTier { get; set; }
    public OptimizationGoal Goal { get; set; }
    public double TotalEstimatedCostChaos { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Single gear upgrade recommendation
/// </summary>
public class GearUpgradeRecommendation
{
    public GearSlot Slot { get; set; }
    public UpgradePriority Priority { get; set; }
    public string CurrentItem { get; set; } = string.Empty;
    public string CurrentItemRarity { get; set; } = string.Empty;
    public string SuggestedItem { get; set; } = string.Empty;
    public string SuggestedItemType { get; set; } = string.Empty;
    public double ImprovementEstimate { get; set; }
    public double EstimatedCostChaos { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<string> AlternativeSuggestions { get; set; } = new();
}

/// <summary>
/// Result of passive tree optimization
/// </summary>
public class PassiveOptimizationResult
{
    public List<PassiveAllocation> SuggestedAllocations { get; set; } = new();
    public List<PassiveRespec> SuggestedRespecs { get; set; } = new();
    public OptimizationGoal Goal { get; set; }
    public int AvailablePoints { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Passive node allocation suggestion
/// </summary>
public class PassiveAllocation
{
    public string NodeName { get; set; } = string.Empty;
    public int NodeId { get; set; }
    public string Benefit { get; set; } = string.Empty;
    public double Score { get; set; }
    public int PathLength { get; set; }
}

/// <summary>
/// Passive node respec suggestion
/// </summary>
public class PassiveRespec
{
    public string CurrentNode { get; set; } = string.Empty;
    public string SuggestedNode { get; set; } = string.Empty;
    public string Benefit { get; set; } = string.Empty;
    public double ImprovementScore { get; set; }
}

/// <summary>
/// Result of skill optimization
/// </summary>
public class SkillOptimizationResult
{
    public List<SkillSetupRecommendation> SuggestedSetups { get; set; } = new();
    public OptimizationGoal Goal { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Skill gem setup recommendation
/// </summary>
public class SkillSetupRecommendation
{
    public string SkillName { get; set; } = string.Empty;
    public List<string> SupportGems { get; set; } = new();
    public UpgradePriority Priority { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public int TotalSpiritCost { get; set; }
    public double EstimatedDps { get; set; }
}

/// <summary>
/// Statistics for a gem (skill or support)
/// </summary>
public class GemStats
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public double BaseDamageMin { get; set; }
    public double BaseDamageMax { get; set; }
    public double CastTime { get; set; } = 1.0;
    public double CritChance { get; set; }
    public double DamageEffectiveness { get; set; } = 100.0;
    public double ManaCost { get; set; }
    public int SpiritCost { get; set; }
}

/// <summary>
/// Support gem effect modifiers
/// </summary>
public class SupportGemEffect
{
    public string Name { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();

    // More multipliers (multiplicative bonuses)
    public double MoreDamage { get; set; }
    public double MoreCastSpeed { get; set; }
    public double MoreAoe { get; set; }
    public double MoreCritChance { get; set; }
    public double MoreCritDamage { get; set; }

    // Less multipliers (multiplicative penalties)
    public double LessDamage { get; set; }
    public double LessCastSpeed { get; set; }

    // Increased modifiers (additive)
    public double IncreasedDamage { get; set; }
    public double IncreasedCastSpeed { get; set; }
    public double IncreasedAoe { get; set; }
    public double IncreasedCritChance { get; set; }

    // Added flat damage
    public double AddedDamageMin { get; set; }
    public double AddedDamageMax { get; set; }
    public string DamageType { get; set; } = string.Empty;

    // Costs
    public int SpiritCost { get; set; }
    public double ManaCostMultiplier { get; set; } = 100.0;

    // Utility
    public List<string> UtilityEffects { get; set; } = new();

    // Requirements
    public List<string> RequiredTags { get; set; } = new();
    public List<string> IncompatibleWith { get; set; } = new();
}

/// <summary>
/// Result of gem synergy calculation
/// </summary>
public class SynergyResult
{
    public string SpellName { get; set; } = string.Empty;
    public List<string> SupportGems { get; set; } = new();

    // DPS metrics
    public double TotalDps { get; set; }
    public double AverageHit { get; set; }
    public double CastsPerSecond { get; set; }

    // Costs
    public int TotalSpiritCost { get; set; }
    public double TotalManaCost { get; set; }

    // Multipliers
    public double TotalMoreMultiplier { get; set; } = 1.0;
    public double TotalIncreasedDamage { get; set; }

    // Utility
    public List<string> UtilityEffects { get; set; } = new();

    // Scoring
    public double DpsScore { get; set; }
    public double EfficiencyScore { get; set; }
    public double OverallScore { get; set; }

    // Breakdown
    public Dictionary<string, object> CalculationBreakdown { get; set; } = new();
}

/// <summary>
/// Character modifiers for gem synergy calculations
/// </summary>
public class CharacterModifiers
{
    public double IncreasedDamage { get; set; }
    public double IncreasedCastSpeed { get; set; }
    public double IncreasedCritChance { get; set; }
    public double IncreasedCritDamage { get; set; }
    public double MoreDamage { get; set; } = 1.0;
    public double MoreCastSpeed { get; set; } = 1.0;
}
