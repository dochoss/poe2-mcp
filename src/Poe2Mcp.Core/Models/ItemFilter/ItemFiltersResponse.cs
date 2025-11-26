namespace Poe2Mcp.Core.Models.ItemFilter;

/// <summary>
/// Response from GET /item-filter endpoint (list)
/// </summary>
public class ItemFiltersResponse
{
    public List<ItemFilter> Filters { get; set; } = new();
}
