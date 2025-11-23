namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Passive tree nodes
/// </summary>
public class PassiveNode
{
    public int Id { get; set; }
    
    public int NodeId { get; set; }
    
    public string? Name { get; set; }
    
    public bool IsKeystone { get; set; }
    
    public bool IsNotable { get; set; }
    
    public bool IsMastery { get; set; }
    
    /// <summary>
    /// JSON serialized list of stat modifiers
    /// </summary>
    public string? Stats { get; set; }
    
    public string? ReminderText { get; set; }
    
    public string? AscendancyName { get; set; }
    
    public double? PositionX { get; set; }
    
    public double? PositionY { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
