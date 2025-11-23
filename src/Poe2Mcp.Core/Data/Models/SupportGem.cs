namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Support gem data
/// </summary>
public class SupportGem
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized list of tags
    /// </summary>
    public string? Tags { get; set; }
    
    public int? RequiredLevel { get; set; }
    
    public double? ManaMultiplier { get; set; }
    
    /// <summary>
    /// JSON serialized list of modifiers
    /// </summary>
    public string? Modifiers { get; set; }
    
    /// <summary>
    /// JSON serialized quality stats
    /// </summary>
    public string? QualityStats { get; set; }
    
    /// <summary>
    /// JSON serialized list of compatible skill tags
    /// </summary>
    public string? CompatibleTags { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
