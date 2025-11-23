namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Item base types and data
/// </summary>
public class Item
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string BaseType { get; set; } = string.Empty;
    
    public string ItemClass { get; set; } = string.Empty;
    
    public string? Rarity { get; set; }
    
    public int? ItemLevel { get; set; }
    
    public int? RequiredLevel { get; set; }
    
    /// <summary>
    /// JSON serialized dictionary of properties
    /// </summary>
    public string? Properties { get; set; }
    
    /// <summary>
    /// JSON serialized stat requirements
    /// </summary>
    public string? Requirements { get; set; }
    
    /// <summary>
    /// JSON serialized list of implicit modifiers
    /// </summary>
    public string? ImplicitMods { get; set; }
    
    /// <summary>
    /// JSON serialized list of explicit modifiers
    /// </summary>
    public string? ExplicitMods { get; set; }
    
    public string? FlavourText { get; set; }
    
    public bool IsCorrupted { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
