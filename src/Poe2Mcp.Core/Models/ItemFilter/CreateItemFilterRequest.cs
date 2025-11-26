namespace Poe2Mcp.Core.Models.ItemFilter;

/// <summary>
/// Request body for POST /item-filter (create)
/// </summary>
public class CreateItemFilterRequest
{
    /// <summary>
    /// Filter display name (required)
    /// </summary>
    public string FilterName { get; set; } = string.Empty;

    /// <summary>
    /// Realm: pc, xbox, sony, or poe2 (required)
    /// </summary>
    public string Realm { get; set; } = string.Empty;

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
    /// Whether the filter is public (optional, defaults to false)
    /// </summary>
    public bool? Public { get; set; }

    /// <summary>
    /// Filter script content (required)
    /// </summary>
    public string Filter { get; set; } = string.Empty;
}
