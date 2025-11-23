namespace Poe2Mcp.Core.Models;

/// <summary>
/// Types of Spirit sources in Path of Exile 2.
/// </summary>
public enum SpiritSourceType
{
    /// <summary>From skull quests (30, 30, 40)</summary>
    Quest,
    
    /// <summary>From equipment</summary>
    Gear,
    
    /// <summary>From passive tree nodes</summary>
    PassiveTree,
    
    /// <summary>From ascendancy nodes</summary>
    Ascendancy,
    
    /// <summary>Temporary Spirit buffs</summary>
    Buff,
    
    /// <summary>Other sources</summary>
    Other
}

/// <summary>
/// Types of Spirit reservations in Path of Exile 2.
/// </summary>
public enum SpiritReservationType
{
    /// <summary>Zombies, Spectres, Golems</summary>
    PermanentMinion,
    
    /// <summary>Auras and buffs</summary>
    Aura,
    
    /// <summary>Meta-gems that persist</summary>
    MetaGem,
    
    /// <summary>Other Spirit-consuming effects</summary>
    Other
}

/// <summary>
/// Represents a source of maximum Spirit.
/// </summary>
public class SpiritSource
{
    /// <summary>
    /// Source name (e.g., "First Skull Quest", "Helmet +30 Spirit").
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Amount of Spirit provided.
    /// </summary>
    public required int Amount { get; init; }
    
    /// <summary>
    /// Type of source.
    /// </summary>
    public required SpiritSourceType SourceType { get; init; }
    
    /// <summary>
    /// Whether this source is active.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Represents a support gem with its cost multiplier.
/// </summary>
public class SupportGem
{
    /// <summary>
    /// Support gem name.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Cost multiplier (e.g., 1.5 for 150%, 1.3 for 130%).
    /// </summary>
    public required double Multiplier { get; init; }
}

/// <summary>
/// Represents a single Spirit reservation (minion/aura/meta-gem).
/// </summary>
public class SpiritReservation
{
    /// <summary>
    /// Reservation name.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Base Spirit cost.
    /// </summary>
    public required int BaseCost { get; init; }
    
    /// <summary>
    /// Type of reservation.
    /// </summary>
    public required SpiritReservationType ReservationType { get; init; }
    
    /// <summary>
    /// List of support gems affecting this reservation.
    /// </summary>
    public List<SupportGem> SupportGems { get; init; } = new();
    
    /// <summary>
    /// Whether this reservation is active.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Priority for auto-disable (lower = higher priority to keep, 1-10).
    /// </summary>
    public int Priority { get; init; } = 5;
    
    /// <summary>
    /// Calculate actual Spirit cost with support gem multipliers.
    /// Spirit costs always round UP in PoE2.
    /// </summary>
    public int CalculateCost()
    {
        if (!Enabled)
            return 0;
            
        double cost = BaseCost;
        foreach (var supportGem in SupportGems)
        {
            cost *= supportGem.Multiplier;
        }
        
        return (int)Math.Ceiling(cost);
    }
    
    /// <summary>
    /// Get detailed breakdown of cost calculation.
    /// </summary>
    public SpiritCostBreakdown GetCostBreakdown()
    {
        var totalMultiplier = 1.0;
        foreach (var supportGem in SupportGems)
        {
            totalMultiplier *= supportGem.Multiplier;
        }
        
        return new SpiritCostBreakdown
        {
            BaseCost = BaseCost,
            SupportGems = SupportGems.Select(sg => new SupportGemInfo
            {
                Name = sg.Name,
                Multiplier = sg.Multiplier
            }).ToList(),
            TotalMultiplier = totalMultiplier,
            RawCost = BaseCost * totalMultiplier,
            FinalCost = CalculateCost(),
            Enabled = Enabled
        };
    }
}

/// <summary>
/// Breakdown of Spirit cost calculation.
/// </summary>
public class SpiritCostBreakdown
{
    public required int BaseCost { get; init; }
    public required List<SupportGemInfo> SupportGems { get; init; }
    public required double TotalMultiplier { get; init; }
    public required double RawCost { get; init; }
    public required int FinalCost { get; init; }
    public required bool Enabled { get; init; }
}

/// <summary>
/// Support gem information for breakdown.
/// </summary>
public class SupportGemInfo
{
    public required string Name { get; init; }
    public required double Multiplier { get; init; }
}

/// <summary>
/// Represents an optimization suggestion for Spirit management.
/// </summary>
public class SpiritOptimization
{
    /// <summary>
    /// Human-readable description.
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// Amount of Spirit that would be saved.
    /// </summary>
    public required int SpiritSaved { get; init; }
    
    /// <summary>
    /// Type of action (disable, remove_support, etc.).
    /// </summary>
    public required string ActionType { get; init; }
    
    /// <summary>
    /// Target reservation/support name.
    /// </summary>
    public required string Target { get; init; }
}

/// <summary>
/// Complete Spirit summary.
/// </summary>
public class SpiritSummary
{
    public required int MaximumSpirit { get; init; }
    public required int ReservedSpirit { get; init; }
    public required int AvailableSpirit { get; init; }
    public required bool IsOverflowing { get; init; }
    public required int OverflowAmount { get; init; }
    public required double UtilizationPercent { get; init; }
    public required Dictionary<string, int> SourceBreakdown { get; init; }
    public required Dictionary<string, int> ReservationCounts { get; init; }
    public required int ActiveReservations { get; init; }
    public required int TotalReservations { get; init; }
}

/// <summary>
/// Optimal Spirit configuration suggestion.
/// </summary>
public class OptimalSpiritConfiguration
{
    public required int MaximumSpirit { get; init; }
    public required int OptimalSpiritUsed { get; init; }
    public required int OptimalSpiritRemaining { get; init; }
    public required List<string> EnabledReservations { get; init; }
    public required List<string> DisabledReservations { get; init; }
    public required double EfficiencyPercent { get; init; }
}
