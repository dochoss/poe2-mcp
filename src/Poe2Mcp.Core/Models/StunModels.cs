namespace Poe2Mcp.Core.Models;

/// <summary>
/// Attack type enumeration for stun bonus calculation.
/// </summary>
public enum AttackType
{
    Melee,
    Ranged,
    Spell
}

/// <summary>
/// Current stun state of an entity.
/// </summary>
public enum StunState
{
    Normal,
    /// <summary>50-99% Heavy Stun meter</summary>
    Primed,
    HeavyStunned
}

/// <summary>
/// Modifiers affecting stun calculations.
/// </summary>
public class StunModifiers
{
    /// <summary>
    /// Additive increased stun chance (e.g., 50 for +50%).
    /// </summary>
    public double IncreasedStunChance { get; set; } = 0.0;
    
    /// <summary>
    /// Multiplicative more stun chance (e.g., 1.5 for 50% more).
    /// </summary>
    public double MoreStunChance { get; set; } = 1.0;
    
    /// <summary>
    /// Enemy stun threshold multiplier (e.g., 1.2 for 20% increased).
    /// </summary>
    public double IncreasedStunThreshold { get; set; } = 1.0;
    
    /// <summary>
    /// Enemy stun threshold reduction (e.g., 0.8 for 20% reduced).
    /// </summary>
    public double ReducedStunThreshold { get; set; } = 1.0;
    
    /// <summary>
    /// Multiplier for Heavy Stun buildup rate.
    /// </summary>
    public double StunBuildupMultiplier { get; set; } = 1.0;
    
    /// <summary>
    /// Minimum chance to stun (overrides 15% default).
    /// </summary>
    public double? MinimumStunChance { get; set; }
    
    /// <summary>
    /// Complete immunity to stuns.
    /// </summary>
    public bool ImmuneToStun { get; set; } = false;
}

/// <summary>
/// Result of a Light Stun chance calculation.
/// </summary>
public class LightStunResult
{
    /// <summary>
    /// Base stun chance before modifiers (%).
    /// </summary>
    public double BaseChance { get; set; }
    
    /// <summary>
    /// Bonus from damage type (e.g., 1.5 for physical).
    /// </summary>
    public double DamageTypeBonus { get; set; }
    
    /// <summary>
    /// Bonus from attack type (e.g., 1.5 for melee).
    /// </summary>
    public double AttackTypeBonus { get; set; }
    
    /// <summary>
    /// Final chance after all modifiers (%).
    /// </summary>
    public double FinalChance { get; set; }
    
    /// <summary>
    /// Whether the stun will occur (after threshold check).
    /// </summary>
    public bool WillStun { get; set; }
    
    /// <summary>
    /// Damage dealt for this calculation.
    /// </summary>
    public double Damage { get; set; }
    
    /// <summary>
    /// Target's maximum life.
    /// </summary>
    public double TargetMaxLife { get; set; }
    
    public override string ToString() =>
        $"Light Stun: {FinalChance:F1}% chance " +
        $"({(WillStun ? "STUN" : "no stun")}) - " +
        $"Base: {BaseChance:F1}% × " +
        $"DmgType: {DamageTypeBonus:F2}× " +
        $"AtkType: {AttackTypeBonus:F2}×";
}

/// <summary>
/// Heavy Stun buildup meter tracking.
/// </summary>
public class HeavyStunMeter
{
    /// <summary>
    /// Current buildup amount.
    /// </summary>
    public double CurrentBuildup { get; set; } = 0.0;
    
    /// <summary>
    /// Maximum buildup (equals target max life).
    /// </summary>
    public double MaxBuildup { get; set; } = 0.0;
    
    /// <summary>
    /// Current buildup as percentage (0-100).
    /// </summary>
    public double BuildupPercentage { get; set; } = 0.0;
    
    /// <summary>
    /// Current stun state (normal, primed, heavy_stunned).
    /// </summary>
    public StunState State { get; set; } = StunState.Normal;
    
    /// <summary>
    /// Number of hits contributing to buildup.
    /// </summary>
    public int HitsReceived { get; set; } = 0;
    
    /// <summary>
    /// History of hits and their buildup contributions.
    /// </summary>
    public List<HitHistory> HitHistoryList { get; set; } = new();
    
    /// <summary>
    /// Update the stun state based on current buildup percentage.
    /// </summary>
    public void UpdateState()
    {
        if (BuildupPercentage >= 100.0)
            State = StunState.HeavyStunned;
        else if (BuildupPercentage >= 50.0)
            State = StunState.Primed;
        else
            State = StunState.Normal;
    }
    
    /// <summary>
    /// Check if the entity is in Primed state (50-99% meter).
    /// </summary>
    public bool IsPrimed() => State == StunState.Primed;
    
    /// <summary>
    /// Check if the entity is Heavy Stunned.
    /// </summary>
    public bool IsHeavyStunned() => State == StunState.HeavyStunned;
    
    /// <summary>
    /// Reset the Heavy Stun meter (e.g., after stun expires).
    /// </summary>
    public void Reset()
    {
        CurrentBuildup = 0.0;
        BuildupPercentage = 0.0;
        State = StunState.Normal;
        HitsReceived = 0;
        HitHistoryList.Clear();
    }
    
    public override string ToString() =>
        $"Heavy Stun Meter: {BuildupPercentage:F1}% " +
        $"({CurrentBuildup:F0}/{MaxBuildup:F0}) - " +
        $"State: {State.ToString().ToUpper()}";
}

/// <summary>
/// History entry for a single hit.
/// </summary>
public class HitHistory
{
    public double Damage { get; set; }
    public double Buildup { get; set; }
    public double TotalBuildup { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Result of a Heavy Stun buildup calculation.
/// </summary>
public class HeavyStunResult
{
    /// <summary>
    /// Amount of buildup added by this hit.
    /// </summary>
    public double BuildupAdded { get; set; }
    
    /// <summary>
    /// Total buildup after this hit.
    /// </summary>
    public double TotalBuildup { get; set; }
    
    /// <summary>
    /// Current Heavy Stun meter state.
    /// </summary>
    public HeavyStunMeter Meter { get; set; } = new();
    
    /// <summary>
    /// Whether this hit triggered Heavy Stun.
    /// </summary>
    public bool TriggeredHeavyStun { get; set; }
    
    /// <summary>
    /// Whether this hit triggered Crushing Blow (primed + would stun).
    /// </summary>
    public bool TriggeredCrushingBlow { get; set; }
    
    /// <summary>
    /// Estimated hits needed to reach Heavy Stun with similar damage.
    /// </summary>
    public double HitsToHeavyStun { get; set; }
    
    public override string ToString()
    {
        var status = new List<string>();
        if (TriggeredHeavyStun)
            status.Add("HEAVY STUN TRIGGERED");
        if (TriggeredCrushingBlow)
            status.Add("CRUSHING BLOW");
        
        var statusStr = status.Count > 0 ? string.Join(" | ", status) : "Building...";
        
        return $"Heavy Stun: +{BuildupAdded:F0} buildup " +
               $"({Meter.BuildupPercentage:F1}%) - {statusStr} - " +
               $"~{HitsToHeavyStun:F1} hits to Heavy Stun";
    }
}

/// <summary>
/// Complete stun calculation result including both Light and Heavy stun.
/// </summary>
public class CompleteStunResult
{
    /// <summary>
    /// Light Stun calculation result.
    /// </summary>
    public LightStunResult LightStun { get; set; } = new();
    
    /// <summary>
    /// Heavy Stun calculation result.
    /// </summary>
    public HeavyStunResult HeavyStun { get; set; } = new();
    
    /// <summary>
    /// Damage dealt.
    /// </summary>
    public double Damage { get; set; }
    
    /// <summary>
    /// Target's maximum life.
    /// </summary>
    public double TargetMaxLife { get; set; }
    
    /// <summary>
    /// Type of damage dealt.
    /// </summary>
    public DamageType DamageType { get; set; }
    
    /// <summary>
    /// Type of attack used.
    /// </summary>
    public AttackType AttackType { get; set; }
    
    public override string ToString() =>
        $"{LightStun}\n{HeavyStun}";
}
