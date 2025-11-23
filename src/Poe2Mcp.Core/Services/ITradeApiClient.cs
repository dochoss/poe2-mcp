using Poe2Mcp.Core.Models;

namespace Poe2Mcp.Core.Services;

/// <summary>
/// Client for the official Path of Exile Trade API
/// </summary>
public interface ITradeApiClient
{
    /// <summary>
    /// Search for items on the trade market
    /// </summary>
    /// <param name="league">League name</param>
    /// <param name="query">Search query object</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of item listings with pricing and details</returns>
    Task<IReadOnlyList<TradeItemListing>> SearchItemsAsync(
        string league,
        TradeSearchQuery query,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Trade search query parameters
/// </summary>
public class TradeSearchQuery
{
    /// <summary>
    /// Search term for the item name.
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// Item type filter (e.g., "Two Hand Sword").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Explicit item name filter.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Stat filters to apply to the search. Each filter specifies a stat id and optional min/max values.
    /// </summary>
    public List<TradeStatFilter>? Stats { get; set; }

    /// <summary>
    /// Additional item filters as key-value pairs (e.g., ilvl, quality, corrupted).
    /// </summary>
    public Dictionary<string, object>? ItemFilters { get; set; }
}

/// <summary>
/// Stat filter for trade search
/// </summary>
public class TradeStatFilter
{
    public string Id { get; set; } = string.Empty;
    public double? Min { get; set; }
    public double? Max { get; set; }
}
