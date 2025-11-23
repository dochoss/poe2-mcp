namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Skill gem data
/// </summary>
public class SkillGem
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? GemType { get; set; }
    
    /// <summary>
    /// JSON serialized list of tags
    /// </summary>
    public string? Tags { get; set; }
    
    public string? PrimaryAttribute { get; set; }
    
    public int? RequiredLevel { get; set; }
    
    public int? ManaCost { get; set; }
    
    /// <summary>
    /// JSON serialized base damage data
    /// </summary>
    public string? BaseDamage { get; set; }
    
    public double? DamageEffectiveness { get; set; }
    
    public double? CritChance { get; set; }
    
    public double? AttackSpeed { get; set; }
    
    /// <summary>
    /// JSON serialized quality stats
    /// </summary>
    public string? QualityStats { get; set; }
    
    /// <summary>
    /// JSON serialized stats at each gem level
    /// </summary>
    public string? PerLevelStats { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
