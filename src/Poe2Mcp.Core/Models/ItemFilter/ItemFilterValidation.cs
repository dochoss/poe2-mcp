namespace Poe2Mcp.Core.Models.ItemFilter;

/// <summary>
/// Item filter validation information
/// </summary>
public class ItemFilterValidation
{
    /// <summary>
    /// Whether the filter is valid
    /// </summary>
    public bool Valid { get; set; }

    /// <summary>
    /// Game version used for validation
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Validation timestamp (ISO8601)
    /// </summary>
    public string? Validated { get; set; }
}
