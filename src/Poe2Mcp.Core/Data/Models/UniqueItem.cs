namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Unique item data
/// </summary>
public class UniqueItem
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string BaseType { get; set; } = string.Empty;
    
    public string ItemClass { get; set; } = string.Empty;
    
    public int? RequiredLevel { get; set; }
    
    /// <summary>
    /// JSON serialized list of unique modifiers
    /// </summary>
    public string? Modifiers { get; set; }
    
    /// <summary>
    /// JSON serialized item stats including chaos_value
    /// </summary>
    public string? Stats { get; set; }
    
    public string? FlavourText { get; set; }
    
    public string? Description { get; set; }
    
    public int? DropLevel { get; set; }
    
    public int? RarityTier { get; set; }
    
    /// <summary>
    /// JSON serialized list of tags
    /// </summary>
    public string? Tags { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
