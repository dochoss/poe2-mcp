namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// User-saved builds
/// </summary>
public class SavedBuild
{
    public int Id { get; set; }
    
    public string? UserId { get; set; }
    
    public string BuildName { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized character data
    /// </summary>
    public string? CharacterData { get; set; }
    
    /// <summary>
    /// JSON serialized list of optimization results
    /// </summary>
    public string? OptimizationHistory { get; set; }
    
    public string? Notes { get; set; }
    
    /// <summary>
    /// JSON serialized list of tags
    /// </summary>
    public string? Tags { get; set; }
    
    public bool IsPublic { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
