namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Item and passive modifiers
/// </summary>
public class Modifier
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string ModType { get; set; } = string.Empty;
    
    public string? StatText { get; set; }
    
    public double? MinValue { get; set; }
    
    public double? MaxValue { get; set; }
    
    /// <summary>
    /// JSON serialized list of tags
    /// </summary>
    public string? Tags { get; set; }
    
    public int? Tier { get; set; }
    
    public int? ItemLevelRequirement { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
