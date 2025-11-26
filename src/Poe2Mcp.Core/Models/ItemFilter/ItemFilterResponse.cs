namespace Poe2Mcp.Core.Models.ItemFilter;

/// <summary>
/// Response from GET /item-filter/{id} endpoint
/// </summary>
public class ItemFilterResponse
{
    public ItemFilter? Filter { get; set; }
}
