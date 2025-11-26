namespace Poe2Mcp.Core.Models.CurrencyExchange;

/// <summary>
/// Currency exchange market data from GET /currency-exchange
/// Official PoE API Reference: https://www.pathofexile.com/developer/docs/reference#currency-exchange
/// </summary>
public class CurrencyExchangeMarket
{
    /// <summary>
    /// League name
    /// </summary>
    public string League { get; set; } = string.Empty;

    /// <summary>
    /// Market identifier (e.g., "chaos|divine")
    /// Common currency code for each pair separated by a pipe
    /// </summary>
    public string MarketId { get; set; } = string.Empty;

    /// <summary>
    /// Volume traded for each currency in the pair
    /// Keys are the market currencies (e.g., "chaos", "divine")
    /// </summary>
    public Dictionary<string, uint> VolumeTrad { get; set; } = new();

    /// <summary>
    /// Lowest stock for each currency
    /// </summary>
    public Dictionary<string, uint> LowestStock { get; set; } = new();

    /// <summary>
    /// Highest stock for each currency
    /// </summary>
    public Dictionary<string, uint> HighestStock { get; set; } = new();

    /// <summary>
    /// Lowest exchange ratio for each currency
    /// </summary>
    public Dictionary<string, uint> LowestRatio { get; set; } = new();

    /// <summary>
    /// Highest exchange ratio for each currency
    /// </summary>
    public Dictionary<string, uint> HighestRatio { get; set; } = new();
}
