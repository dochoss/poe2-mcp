namespace Poe2Mcp.Core.Models;

/// <summary>
/// Statistics provided by a piece of gear.
/// </summary>
public class GearStats
{
    // Defense stats
    public double Armor { get; set; }
    public double Evasion { get; set; }
    public double EnergyShield { get; set; }

    // Life/Mana
    public double Life { get; set; }
    public double Mana { get; set; }

    // Resistances
    public double FireRes { get; set; }
    public double ColdRes { get; set; }
    public double LightningRes { get; set; }
    public double ChaosRes { get; set; }

    // Attributes
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Intelligence { get; set; }

    // Damage mods (simplified)
    public double IncreasedDamage { get; set; }
    public double MoreDamage { get; set; } = 1.0;
    public double AddedFlatDamage { get; set; }
    public double CritChance { get; set; }
    public double CritMulti { get; set; }

    // Special
    public int Spirit { get; set; }
    public double BlockChance { get; set; }

    // Item metadata
    public string ItemName { get; set; } = string.Empty;
    public string ItemSlot { get; set; } = string.Empty;
    public int ItemLevel { get; set; }
}

/// <summary>
/// Complete character stats (base + gear).
/// </summary>
public class CharacterStats
{
    // Defense stats
    public double Armor { get; set; }
    public double Evasion { get; set; }
    public double EnergyShield { get; set; }

    // Life/Mana/Spirit
    public double Life { get; set; } = 1; // Must be > 0
    public double Mana { get; set; }
    public int Spirit { get; set; }

    // Resistances
    public double FireRes { get; set; }
    public double ColdRes { get; set; }
    public double LightningRes { get; set; }
    public double ChaosRes { get; set; }

    // Attributes
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Intelligence { get; set; }

    // Damage mods
    public double IncreasedDamage { get; set; }
    public double MoreDamage { get; set; } = 1.0;
    public double AddedFlatDamage { get; set; }
    public double CritChance { get; set; }
    public double CritMulti { get; set; }

    // Special
    public double BlockChance { get; set; }
}

/// <summary>
/// Upgrade recommendation levels.
/// </summary>
public enum UpgradeRecommendation
{
    /// <summary>Significant improvement</summary>
    StrongUpgrade,
    /// <summary>Moderate improvement</summary>
    Upgrade,
    /// <summary>Mixed changes, roughly equal</summary>
    Sidegrade,
    /// <summary>No improvement</summary>
    Skip,
    /// <summary>Worse than current</summary>
    Downgrade
}

/// <summary>
/// Calculated value of a gear upgrade.
/// </summary>
public class UpgradeValue
{
    /// <summary>EHP changes for each damage type (absolute and percent)</summary>
    public Dictionary<string, Dictionary<string, double>> EhpChanges { get; set; } = new();

    /// <summary>DPS change (absolute and percent)</summary>
    public Dictionary<string, double> DpsChange { get; set; } = new();

    /// <summary>Resistance changes</summary>
    public Dictionary<string, double> ResistanceChanges { get; set; } = new();

    /// <summary>Raw stat changes</summary>
    public Dictionary<string, double> StatChanges { get; set; } = new();

    /// <summary>Overall priority score (0-100)</summary>
    public double PriorityScore { get; set; }

    /// <summary>Upgrade recommendation</summary>
    public UpgradeRecommendation Recommendation { get; set; }

    /// <summary>Estimated value in chaos orbs (if applicable)</summary>
    public double? TradeValue { get; set; }

    /// <summary>Warnings about the upgrade</summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of comparing two items directly.
/// </summary>
public class ItemComparison
{
    public string ItemA { get; set; } = string.Empty;
    public double ItemAScore { get; set; }
    public UpgradeValue ItemAValue { get; set; } = new();

    public string ItemB { get; set; } = string.Empty;
    public double ItemBScore { get; set; }
    public UpgradeValue ItemBValue { get; set; } = new();

    public string Winner { get; set; } = string.Empty;
    public double ScoreDifference { get; set; }
}

/// <summary>
/// Build tier classification.
/// </summary>
public enum BuildTier
{
    S,
    A,
    B,
    C,
    D,
    F
}

/// <summary>
/// Build scoring result.
/// </summary>
public class BuildScore
{
    public double OverallScore { get; set; }
    public BuildTier Tier { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public double GearScore { get; set; }
    public double PassiveScore { get; set; }
    public double SkillScore { get; set; }
    public double Dps { get; set; }
    public double Ehp { get; set; }
    public double DefenseRating { get; set; }
}

/// <summary>
/// Content difficulty tiers.
/// </summary>
public enum ContentDifficulty
{
    Campaign,
    EarlyMaps,      // T1-T5
    MidMaps,        // T6-T10
    HighMaps,       // T11-T15
    PinnacleMaps,   // T16
    BossNormal,
    BossPinnacle
}

/// <summary>
/// Readiness assessment levels.
/// </summary>
public enum ReadinessLevel
{
    Ready,
    MostlyReady,
    Risky,
    NotReady
}

/// <summary>
/// Defense requirements for specific content.
/// </summary>
public class DefenseRequirement
{
    public string ContentName { get; set; } = string.Empty;
    public ContentDifficulty Difficulty { get; set; }

    // Minimum requirements
    public int MinLife { get; set; }
    public int MinEhp { get; set; }
    public int MinFireRes { get; set; }
    public int MinColdRes { get; set; }
    public int MinLightningRes { get; set; }
    public int MinChaosRes { get; set; }

    // Recommended
    public int RecLife { get; set; }
    public int RecEhp { get; set; }
    public int RecFireRes { get; set; }
    public int RecColdRes { get; set; }
    public int RecLightningRes { get; set; }
    public int RecChaosRes { get; set; }

    // Additional requirements
    public bool RequiresStunImmunity { get; set; }
    public bool RequiresFreezeImmunity { get; set; }
    public bool RequiresCurseImmunity { get; set; }
    public double MinPhysMitigation { get; set; }
    public double MinEleMitigation { get; set; }

    // Damage requirements
    public int RecDps { get; set; }
    public int MinDps { get; set; }

    // Notes
    public List<string> DangerousMechanics { get; set; } = new();
    public List<string> Tips { get; set; } = new();
}

/// <summary>
/// Report on whether character is ready for content.
/// </summary>
public class ReadinessReport
{
    public string ContentName { get; set; } = string.Empty;
    public ReadinessLevel Readiness { get; set; }
    public double Confidence { get; set; } // 0-100

    // Assessment breakdown
    public string LifeCheck { get; set; } = string.Empty; // "pass", "warning", "fail"
    public string EhpCheck { get; set; } = string.Empty;
    public string ResistanceCheck { get; set; } = string.Empty;
    public string DamageCheck { get; set; } = string.Empty;
    public string ImmunityCheck { get; set; } = string.Empty;

    // Detailed gaps
    public List<string> Gaps { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Passes { get; set; } = new();

    // Recommendations
    public List<string> Recommendations { get; set; } = new();
    public List<string> PriorityUpgrades { get; set; } = new();
}
