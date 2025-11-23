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
    public string? Term { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public List<TradeStatFilter>? Stats { get; set; }
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
