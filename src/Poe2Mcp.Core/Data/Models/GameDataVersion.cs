namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Track game data version
/// </summary>
public class GameDataVersion
{
    public int Id { get; set; }
    
    public string DataSource { get; set; } = string.Empty;
    
    public string? Version { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public string? DataHash { get; set; }
}
