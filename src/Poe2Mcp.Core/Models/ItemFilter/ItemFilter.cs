namespace Poe2Mcp.Core.Models.ItemFilter;

/// <summary>
/// Item filter data from GET /item-filter endpoints
/// Official PoE API Reference: https://www.pathofexile.com/developer/docs/reference#account-item-filters
/// </summary>
public class ItemFilter
{
    /// <summary>
    /// Item filter identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Filter display name
    /// </summary>
    public string FilterName { get; set; } = string.Empty;

    /// <summary>
    /// Realm (pc, xbox, sony, poe2)
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Filter description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Filter version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Filter type: "Normal" or "Ruthless"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether the filter is public
    /// </summary>
    public bool? Public { get; set; }

    /// <summary>
    /// Filter script content (only present in detailed GET requests)
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Validation information (only present in detailed GET requests)
    /// </summary>
    public ItemFilterValidation? Validation { get; set; }
}
