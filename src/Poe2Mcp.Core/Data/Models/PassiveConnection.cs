namespace Poe2Mcp.Core.Data.Models;

/// <summary>
/// Connections between passive nodes
/// </summary>
public class PassiveConnection
{
    public int Id { get; set; }
    
    public int FromNodeId { get; set; }
    
    public int ToNodeId { get; set; }
    
    public PassiveNode? FromNode { get; set; }
    
    public PassiveNode? ToNode { get; set; }
}
