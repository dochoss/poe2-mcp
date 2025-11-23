namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Ascendancy class data
/// </summary>
public class Ascendancy
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string BaseClass { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    /// <summary>
    /// JSON serialized list of notable passive IDs
    /// </summary>
    public string? NotablePassives { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
