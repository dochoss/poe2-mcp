namespace Poe2Mcp.Core.Models.ItemFilter;

/// <summary>
/// Request body for POST /item-filter/{id} (update)
/// All properties are optional - only provided properties will be updated
/// </summary>
public class UpdateItemFilterRequest
{
    /// <summary>
    /// Filter display name (optional)
    /// </summary>
    public string? FilterName { get; set; }

    /// <summary>
    /// Realm: pc, xbox, sony, or poe2 (optional)
    /// </summary>
    public string? Realm { get; set; }

    /// <summary>
    /// Filter description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Filter version (optional)
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Filter type: Normal or Ruthless (optional)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Whether the filter is public (optional, must be true if present - cannot make public filter private)
    /// </summary>
    public bool? Public { get; set; }

    /// <summary>
    /// Filter script content (optional)
    /// </summary>
    public string? Filter { get; set; }
}
